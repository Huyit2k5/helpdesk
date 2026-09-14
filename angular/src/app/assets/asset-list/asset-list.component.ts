import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { AssetService } from '../../proxy/assets/asset.service';
import {
  AssetDto,
  AssetKpiDto,
  AssetStatus,
  AssetType,
  AssignAssetDto,
  ChangeAssetStatusDto,
  CreateAssetDto,
  GetAssetsInput,
  ReturnAssetDto,
  UpdateAssetDto
} from '../../proxy/assets/models';
import { DepartmentService } from '../../proxy/departments/department.service';
import { DepartmentDto } from '../../proxy/departments/models';
import { IdentityUserService, IdentityUserDto } from '@abp/ng.identity/proxy';
import { catchError, of } from 'rxjs';
import { AssetReceiptModalComponent } from '../components/asset-receipt-modal/asset-receipt-modal.component';
import { QrScannerModalComponent } from '../components/qr-scanner-modal/qr-scanner-modal.component';

@Component({
  selector: 'app-asset-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, AssetReceiptModalComponent, QrScannerModalComponent],
  templateUrl: './asset-list.component.html',
  styleUrls: ['./asset-list.component.scss']
})
export class AssetListComponent implements OnInit {
  private assetService = inject(AssetService);
  private departmentService = inject(DepartmentService);
  private userService = inject(IdentityUserService);
  private toaster = inject(ToasterService);
  private confirmation = inject(ConfirmationService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  assets: AssetDto[] = [];
  departments: DepartmentDto[] = [];
  users: IdentityUserDto[] = [];
  selectedUser: IdentityUserDto | null = null;
  kpis: AssetKpiDto = {
    totalAssets: 0,
    inStockCount: 0,
    assignedCount: 0,
    underRepairCount: 0,
    warrantyExpiringSoonCount: 0
  };

  loading = false;
  totalCount = 0;
  pageSize = 10;
  currentPage = 1;

  // Filter criteria
  filterText = '';
  selectedAssetType: AssetType | null = null;
  selectedStatus: AssetStatus | null = null;
  selectedDepartment = '';
  activeTab: 'all' | 'inStock' | 'assigned' | 'underRepair' | 'warrantyExpiring' = 'all';
  selectedCategoryStock: AssetType | null = null;
  isWarrantyExpiringFilter = false;

  // Enums for template
  AssetType = AssetType;
  AssetStatus = AssetStatus;

  assetTypes = [
    { value: AssetType.Laptop, label: 'Laptop (Máy tính xách tay)', icon: 'fas fa-laptop' },
    { value: AssetType.Desktop, label: 'Desktop (Máy bàn)', icon: 'fas fa-desktop' },
    { value: AssetType.Monitor, label: 'Màn hình hiển thị', icon: 'fas fa-tv' },
    { value: AssetType.NetworkDevice, label: 'Thiết bị mạng (Router/Switch)', icon: 'fas fa-network-wired' },
    { value: AssetType.PrinterPeripheral, label: 'Máy in & Ngoại vi', icon: 'fas fa-print' },
    { value: AssetType.ServerStorage, label: 'Máy chủ & Lưu trữ', icon: 'fas fa-server' },
    { value: AssetType.SoftwareLicense, label: 'Bản quyền phần mềm', icon: 'fas fa-key' },
    { value: AssetType.MobileDevice, label: 'Thiết bị di động', icon: 'fas fa-mobile-alt' },
    { value: AssetType.Other, label: 'Thiết bị khác', icon: 'fas fa-cube' },
  ];

  assetStatuses = [
    { value: AssetStatus.InStock, label: 'Trong kho lưu trữ', badgeClass: 'badge-stock' },
    { value: AssetStatus.Assigned, label: 'Đang cấp phát', badgeClass: 'badge-assigned' },
    { value: AssetStatus.UnderRepair, label: 'Đang sửa chữa / Bảo hành', badgeClass: 'badge-repair' },
    { value: AssetStatus.Reserved, label: 'Đã đặt trước', badgeClass: 'badge-reserved' },
    { value: AssetStatus.Retired, label: 'Đã thanh lý', badgeClass: 'badge-retired' },
    { value: AssetStatus.LostStolen, label: 'Báo mất / Thất lạc', badgeClass: 'badge-lost' },
  ];

  // Modals state
  isCreateModalOpen = false;
  isEditModalOpen = false;
  isAssignModalOpen = false;
  isReturnModalOpen = false;
  isStatusModalOpen = false;
  isReceiptModalOpen = false;
  selectedReceiptAssetId: string | null = null;
  selectedReceiptType: 'handover' | 'return' = 'handover';
  isQrScannerModalOpen = false;

  selectedAsset: AssetDto | null = null;

  assetForm: FormGroup = this.initAssetForm();
  assignForm: FormGroup = this.initAssignForm();
  statusForm: FormGroup = this.initStatusForm();
  returnNotes = '';

  ngOnInit(): void {
    this.loadKpis();
    this.loadDepartments();
    this.loadUsers();
    this.loadAssets();
  }

  loadUsers(): void {
    this.userService.getList({ maxResultCount: 100, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.users = res.items || [];
      this.cdr.markForCheck();
    });
  }

  getUserDisplayName(user: IdentityUserDto): string {
    if (user.name && user.surname) {
      return `${user.surname} ${user.name}`;
    }
    if (user.name) {
      return user.name;
    }
    return user.userName || '';
  }

  hasDepartment(name: string | null | undefined): boolean {
    if (!name) return true;
    return this.departments.some(d => d.name === name);
  }

  onUserSelect(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const userId = target.value;
    if (!userId || userId === 'manual') {
      this.selectedUser = null;
      this.assignForm.patchValue({
        userId: null
      });
      return;
    }

    const found = this.users.find(u => u.id === userId);
    if (found) {
      this.selectedUser = found;
      this.assignForm.patchValue({
        userId: found.id,
        userName: this.getUserDisplayName(found),
        userEmail: found.email || ''
      });
    }
  }

  initAssetForm(): FormGroup {
    return this.fb.group({
      assetTag: [''],
      name: ['', [Validators.required, Validators.maxLength(128)]],
      assetType: [AssetType.Laptop, Validators.required],
      status: [AssetStatus.InStock, Validators.required],
      serialNumber: ['', Validators.maxLength(64)],
      model: ['', Validators.maxLength(128)],
      manufacturer: ['', Validators.maxLength(128)],
      location: ['', Validators.maxLength(128)],
      purchaseDate: [''],
      warrantyExpiryDate: [''],
      purchaseCost: [null],
      specifications: ['', Validators.maxLength(2000)],
      notes: ['', Validators.maxLength(2000)],
    });
  }

  initAssignForm(): FormGroup {
    return this.fb.group({
      userId: [null],
      userName: ['', [Validators.required, Validators.maxLength(128)]],
      userEmail: ['', [Validators.email, Validators.maxLength(128)]],
      department: ['', Validators.maxLength(128)],
      notes: ['', Validators.maxLength(2000)],
    });
  }

  initStatusForm(): FormGroup {
    return this.fb.group({
      status: [AssetStatus.InStock, Validators.required],
      reason: ['', Validators.maxLength(1000)],
    });
  }

  loadKpis(): void {
    this.assetService.getKpis().subscribe({
      next: (kpis) => {
        this.kpis = kpis;
        this.cdr.markForCheck();
      }
    });
  }

  loadDepartments(): void {
    this.departmentService.getList({ maxResultCount: 100 } as any).subscribe({
      next: (res) => {
        this.departments = res.items || [];
        this.cdr.markForCheck();
      }
    });
  }

  loadAssets(): void {
    this.loading = true;
    this.cdr.markForCheck();
    const effectiveAssetType = this.selectedCategoryStock ?? this.selectedAssetType ?? undefined;
    const input: GetAssetsInput = {
      filter: this.filterText || undefined,
      assetType: effectiveAssetType,
      status: this.selectedStatus ?? undefined,
      department: this.selectedDepartment || undefined,
      warrantyExpiringSoon: this.isWarrantyExpiringFilter ? true : undefined,
      skipCount: (this.currentPage - 1) * this.pageSize,
      maxResultCount: this.pageSize,
      sorting: 'CreationTime DESC'
    };

    this.assetService.getList(input).subscribe({
      next: (res) => {
        this.assets = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  onFilter(): void {
    this.currentPage = 1;
    this.loadAssets();
  }

  setFilterTab(tab: 'all' | 'inStock' | 'assigned' | 'underRepair' | 'warrantyExpiring'): void {
    this.activeTab = tab;
    this.currentPage = 1;
    this.selectedCategoryStock = null;
    this.isWarrantyExpiringFilter = false;

    switch (tab) {
      case 'all':
        this.selectedStatus = null;
        break;
      case 'inStock':
        this.selectedStatus = AssetStatus.InStock;
        break;
      case 'assigned':
        this.selectedStatus = AssetStatus.Assigned;
        break;
      case 'underRepair':
        this.selectedStatus = AssetStatus.UnderRepair;
        break;
      case 'warrantyExpiring':
        this.selectedStatus = null;
        this.isWarrantyExpiringFilter = true;
        break;
    }
    this.loadAssets();
  }

  selectKpiFilter(filter: 'total' | 'inStock' | 'assigned' | 'underRepair' | 'warrantyExpiring'): void {
    const tabMap: Record<string, 'all' | 'inStock' | 'assigned' | 'underRepair' | 'warrantyExpiring'> = {
      total: 'all',
      inStock: 'inStock',
      assigned: 'assigned',
      underRepair: 'underRepair',
      warrantyExpiring: 'warrantyExpiring'
    };
    this.setFilterTab(tabMap[filter]);
  }

  filterByStockCategory(type: AssetType | null): void {
    if (this.selectedCategoryStock === type) {
      this.selectedCategoryStock = null;
    } else {
      this.selectedCategoryStock = type;
      if (this.activeTab !== 'inStock') {
        this.activeTab = 'inStock';
        this.selectedStatus = AssetStatus.InStock;
        this.isWarrantyExpiringFilter = false;
      }
    }
    this.currentPage = 1;
    this.loadAssets();
  }

  getStockStatusBadge(inStock: number): { label: string; class: string } {
    if (inStock === 0) {
      return { label: 'Hết hàng trong kho', class: 'bg-danger-subtle text-danger border border-danger-subtle' };
    } else if (inStock === 1) {
      return { label: 'Sắp hết (Còn 1 cái)', class: 'bg-warning-subtle text-warning-emphasis border border-warning-subtle' };
    } else {
      return { label: `Sẵn sàng (${inStock} cái)`, class: 'bg-success-subtle text-success border border-success-subtle' };
    }
  }

  resetFilter(): void {
    this.filterText = '';
    this.selectedAssetType = null;
    this.selectedStatus = null;
    this.selectedDepartment = '';
    this.selectedCategoryStock = null;
    this.isWarrantyExpiringFilter = false;
    this.activeTab = 'all';
    this.currentPage = 1;
    this.loadAssets();
  }

  openCreateModal(): void {
    this.assetForm = this.initAssetForm();
    this.isCreateModalOpen = true;
  }

  openEditModal(asset: AssetDto): void {
    this.selectedAsset = asset;
    this.assetForm = this.fb.group({
      name: [asset.name, [Validators.required, Validators.maxLength(128)]],
      assetType: [asset.assetType, Validators.required],
      serialNumber: [asset.serialNumber || '', Validators.maxLength(64)],
      model: [asset.model || '', Validators.maxLength(128)],
      manufacturer: [asset.manufacturer || '', Validators.maxLength(128)],
      location: [asset.location || '', Validators.maxLength(128)],
      purchaseDate: [asset.purchaseDate ? asset.purchaseDate.substring(0, 10) : ''],
      warrantyExpiryDate: [asset.warrantyExpiryDate ? asset.warrantyExpiryDate.substring(0, 10) : ''],
      purchaseCost: [asset.purchaseCost],
      specifications: [asset.specifications || '', Validators.maxLength(2000)],
      notes: [asset.notes || '', Validators.maxLength(2000)],
    });
    this.isEditModalOpen = true;
  }

  saveAsset(): void {
    if (this.assetForm.invalid) {
      return;
    }

    const formVal = this.assetForm.value;

    if (this.isCreateModalOpen) {
      const createInput: CreateAssetDto = {
        assetTag: formVal.assetTag || null,
        name: formVal.name,
        assetType: Number(formVal.assetType),
        status: Number(formVal.status),
        serialNumber: formVal.serialNumber || null,
        model: formVal.model || null,
        manufacturer: formVal.manufacturer || null,
        location: formVal.location || null,
        purchaseDate: formVal.purchaseDate ? new Date(formVal.purchaseDate).toISOString() : null,
        warrantyExpiryDate: formVal.warrantyExpiryDate ? new Date(formVal.warrantyExpiryDate).toISOString() : null,
        purchaseCost: formVal.purchaseCost ? Number(formVal.purchaseCost) : null,
        specifications: formVal.specifications || null,
        notes: formVal.notes || null,
      };

      this.assetService.create(createInput).subscribe({
        next: () => {
          this.toaster.success('Đã thêm mới tài sản thành công!');
          this.isCreateModalOpen = false;
          this.loadKpis();
          this.loadAssets();
        }
      });
    } else if (this.isEditModalOpen && this.selectedAsset?.id) {
      const updateInput: UpdateAssetDto = {
        name: formVal.name,
        assetType: Number(formVal.assetType),
        serialNumber: formVal.serialNumber || null,
        model: formVal.model || null,
        manufacturer: formVal.manufacturer || null,
        location: formVal.location || null,
        purchaseDate: formVal.purchaseDate ? new Date(formVal.purchaseDate).toISOString() : null,
        warrantyExpiryDate: formVal.warrantyExpiryDate ? new Date(formVal.warrantyExpiryDate).toISOString() : null,
        purchaseCost: formVal.purchaseCost ? Number(formVal.purchaseCost) : null,
        specifications: formVal.specifications || null,
        notes: formVal.notes || null,
      };

      this.assetService.update(this.selectedAsset.id, updateInput).subscribe({
        next: () => {
          this.toaster.success('Cập nhật thông tin tài sản thành công!');
          this.isEditModalOpen = false;
          this.loadAssets();
        }
      });
    }
  }

  openAssignModal(asset: AssetDto): void {
    this.selectedAsset = asset;
    this.selectedUser = this.users.find(u => u.id === asset.assignedToUserId) || null;
    this.assignForm = this.fb.group({
      userId: [asset.assignedToUserId || null],
      userName: [asset.assignedToUserName || '', [Validators.required, Validators.maxLength(128)]],
      userEmail: [asset.assignedToUserEmail || '', [Validators.email, Validators.maxLength(128)]],
      department: [asset.department || '', Validators.maxLength(128)],
      notes: ['', Validators.maxLength(2000)],
    });
    this.isAssignModalOpen = true;
  }

  saveAssign(): void {
    if (this.assignForm.invalid || !this.selectedAsset?.id) {
      return;
    }

    const val = this.assignForm.value;
    const input: AssignAssetDto = {
      userId: val.userId || null,
      userName: val.userName,
      userEmail: val.userEmail || null,
      department: val.department || null,
      notes: val.notes || null,
    };

    this.assetService.assign(this.selectedAsset.id, input).subscribe({
      next: () => {
        this.toaster.success(`Đã cấp phát tài sản cho ${input.userName}!`);
        this.isAssignModalOpen = false;
        this.loadKpis();
        this.loadAssets();
      }
    });
  }

  openReturnModal(asset: AssetDto): void {
    this.selectedAsset = asset;
    this.returnNotes = '';
    this.isReturnModalOpen = true;
  }

  saveReturn(): void {
    if (!this.selectedAsset?.id) return;

    const input: ReturnAssetDto = { notes: this.returnNotes || null };
    this.assetService.return(this.selectedAsset.id, input).subscribe({
      next: () => {
        this.toaster.success('Đã thu hồi thiết bị về kho thành công!');
        this.isReturnModalOpen = false;
        this.loadKpis();
        this.loadAssets();
      }
    });
  }

  openStatusModal(asset: AssetDto): void {
    this.selectedAsset = asset;
    this.statusForm = this.fb.group({
      status: [asset.status, Validators.required],
      reason: ['', Validators.maxLength(1000)],
    });
    this.isStatusModalOpen = true;
  }

  saveStatus(): void {
    if (this.statusForm.invalid || !this.selectedAsset?.id) return;

    const val = this.statusForm.value;
    const input: ChangeAssetStatusDto = {
      status: Number(val.status),
      reason: val.reason || null,
    };

    this.assetService.changeStatus(this.selectedAsset.id, input).subscribe({
      next: () => {
        this.toaster.success('Đã thay đổi trạng thái tài sản!');
        this.isStatusModalOpen = false;
        this.loadKpis();
        this.loadAssets();
      }
    });
  }

  deleteAsset(asset: AssetDto): void {
    if (!asset.id) return;

    this.confirmation.warn(
      `Bạn có chắc chắn muốn xóa tài sản "${asset.name}" (${asset.assetTag}) không?`,
      'Xác nhận xóa tài sản'
    ).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.assetService.delete(asset.id!).subscribe({
          next: () => {
            this.toaster.success('Đã xóa tài sản thành công!');
            this.loadKpis();
            this.loadAssets();
          }
        });
      }
    });
  }

  goToDetail(asset: AssetDto): void {
    this.router.navigate(['/assets', asset.id]);
  }

  getStatusBadgeClass(status: AssetStatus): string {
    switch (status) {
      case AssetStatus.InStock:
        return 'badge-stock';
      case AssetStatus.Assigned:
        return 'badge-assigned';
      case AssetStatus.UnderRepair:
        return 'badge-repair';
      case AssetStatus.Reserved:
        return 'badge-reserved';
      case AssetStatus.Retired:
        return 'badge-retired';
      case AssetStatus.LostStolen:
        return 'badge-lost';
      default:
        return 'badge-stock';
    }
  }

  getTypeIcon(type: AssetType): string {
    switch (type) {
      case AssetType.Laptop:
        return 'fas fa-laptop';
      case AssetType.Desktop:
        return 'fas fa-desktop';
      case AssetType.Monitor:
        return 'fas fa-tv';
      case AssetType.NetworkDevice:
        return 'fas fa-network-wired';
      case AssetType.PrinterPeripheral:
        return 'fas fa-print';
      case AssetType.ServerStorage:
        return 'fas fa-server';
      case AssetType.SoftwareLicense:
        return 'fas fa-key';
      case AssetType.MobileDevice:
        return 'fas fa-mobile-alt';
      default:
        return 'fas fa-cube';
    }
  }

  isWarrantyExpiringSoon(dateStr?: string | null): boolean {
    if (!dateStr) return false;
    const expiry = new Date(dateStr).getTime();
    const now = new Date().getTime();
    const thirtyDays = 30 * 24 * 60 * 60 * 1000;
    return expiry >= now && expiry <= now + thirtyDays;
  }

  getTypeLabel(type: AssetType | null | undefined): string {
    if (type === null || type === undefined) return '';
    const found = this.assetTypes.find(t => t.value === type);
    return found ? found.label : 'Thiết bị';
  }

  isWarrantyExpired(dateStr?: string | null): boolean {
    if (!dateStr) return false;
    const expiry = new Date(dateStr).getTime();
    const now = new Date().getTime();
    return expiry < now;
  }

  openReceiptModal(assetId?: string | null, type: 'handover' | 'return' = 'handover'): void {
    if (!assetId) return;
    this.selectedReceiptAssetId = assetId;
    this.selectedReceiptType = type;
    this.isReceiptModalOpen = true;
  }

  closeReceiptModal(): void {
    this.isReceiptModalOpen = false;
    this.selectedReceiptAssetId = null;
  }

  openQrScanner(): void {
    this.isQrScannerModalOpen = true;
  }

  closeQrScanner(): void {
    this.isQrScannerModalOpen = false;
  }

  onScanOpenReceipt(data: { assetId: string; type: string }): void {
    this.closeQrScanner();
    this.openReceiptModal(data.assetId, data.type as any);
  }
}
