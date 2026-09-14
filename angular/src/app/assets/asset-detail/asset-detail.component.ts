import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { IdentityUserService, IdentityUserDto } from '@abp/ng.identity/proxy';
import { DepartmentService } from '../../proxy/departments/department.service';
import { DepartmentDto } from '../../proxy/departments/models';
import { catchError, of } from 'rxjs';
import { AssetService } from '../../proxy/assets/asset.service';
import { QrCodeGenerator } from '../qr-code-helper';
import {
  AssetActivityDto,
  AssetActivityType,
  AssetDetailDto,
  AssetStatus,
  AssetTicketDto,
  AssetType,
  AssignAssetDto,
  ChangeAssetStatusDto,
  ReturnAssetDto,
  UpdateAssetDto
} from '../../proxy/assets/models';

import { AssetReceiptModalComponent } from '../components/asset-receipt-modal/asset-receipt-modal.component';

@Component({
  selector: 'app-asset-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule, AssetReceiptModalComponent],
  templateUrl: './asset-detail.component.html',
  styleUrls: ['./asset-detail.component.scss']
})
export class AssetDetailComponent implements OnInit {
  private assetService = inject(AssetService);
  private userService = inject(IdentityUserService);
  private departmentService = inject(DepartmentService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private confirmation = inject(ConfirmationService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);
  private sanitizer = inject(DomSanitizer);

  assetId = '';
  asset: AssetDetailDto | null = null;
  loading = true;

  users: IdentityUserDto[] = [];
  departments: DepartmentDto[] = [];
  selectedUser: IdentityUserDto | null = null;

  activeTab: 'tickets' | 'activities' = 'tickets';

  // Modals state
  isEditModalOpen = false;
  isAssignModalOpen = false;
  isReturnModalOpen = false;
  isStatusModalOpen = false;
  isPrintModalOpen = false;
  isReceiptModalOpen = false;
  receiptType: 'handover' | 'return' = 'handover';
  qrCodeSvg: SafeHtml | null = null;

  assetForm: FormGroup = this.initAssetForm();
  assignForm: FormGroup = this.initAssignForm();
  statusForm: FormGroup = this.initStatusForm();
  returnNotes = '';

  AssetStatus = AssetStatus;
  AssetType = AssetType;
  AssetActivityType = AssetActivityType;

  assetTypes = [
    { value: AssetType.Laptop, label: 'Laptop (Máy tính xách tay)' },
    { value: AssetType.Desktop, label: 'Desktop (Máy bàn)' },
    { value: AssetType.Monitor, label: 'Màn hình hiển thị' },
    { value: AssetType.NetworkDevice, label: 'Thiết bị mạng (Router/Switch)' },
    { value: AssetType.PrinterPeripheral, label: 'Máy in & Ngoại vi' },
    { value: AssetType.ServerStorage, label: 'Máy chủ & Lưu trữ' },
    { value: AssetType.SoftwareLicense, label: 'Bản quyền phần mềm' },
    { value: AssetType.MobileDevice, label: 'Thiết bị di động' },
    { value: AssetType.Other, label: 'Thiết bị khác' },
  ];

  assetStatuses = [
    { value: AssetStatus.InStock, label: 'Trong kho lưu trữ' },
    { value: AssetStatus.Assigned, label: 'Đang cấp phát' },
    { value: AssetStatus.UnderRepair, label: 'Đang sửa chữa / Bảo hành' },
    { value: AssetStatus.Reserved, label: 'Đã đặt trước' },
    { value: AssetStatus.Retired, label: 'Đã thanh lý' },
    { value: AssetStatus.LostStolen, label: 'Báo mất / Thất lạc' },
  ];

  ngOnInit(): void {
    this.loadLookups();
    this.route.params.subscribe((params) => {
      this.assetId = params['id'];
      if (this.assetId) {
        this.loadAsset();
      }
    });
  }

  loadLookups(): void {
    this.userService.getList({ maxResultCount: 100, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.users = res.items || [];
      this.cdr.markForCheck();
    });

    this.departmentService.getList({ maxResultCount: 100 } as any).pipe(
      catchError(() => of({ items: [] }))
    ).subscribe(res => {
      this.departments = res.items || [];
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
      name: ['', [Validators.required, Validators.maxLength(128)]],
      assetType: [AssetType.Laptop, Validators.required],
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

  loadAsset(): void {
    this.loading = true;
    this.cdr.markForCheck();
    this.assetService.get(this.assetId).subscribe({
      next: (res) => {
        this.asset = res;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
        this.toaster.error('Không thể tải thông tin tài sản!');
        this.router.navigate(['/assets']);
      }
    });
  }

  openEditModal(): void {
    if (!this.asset) return;
    this.assetForm = this.fb.group({
      name: [this.asset.name, [Validators.required, Validators.maxLength(128)]],
      assetType: [this.asset.assetType, Validators.required],
      serialNumber: [this.asset.serialNumber || '', Validators.maxLength(64)],
      model: [this.asset.model || '', Validators.maxLength(128)],
      manufacturer: [this.asset.manufacturer || '', Validators.maxLength(128)],
      location: [this.asset.location || '', Validators.maxLength(128)],
      purchaseDate: [this.asset.purchaseDate ? this.asset.purchaseDate.substring(0, 10) : ''],
      warrantyExpiryDate: [this.asset.warrantyExpiryDate ? this.asset.warrantyExpiryDate.substring(0, 10) : ''],
      purchaseCost: [this.asset.purchaseCost],
      specifications: [this.asset.specifications || '', Validators.maxLength(2000)],
      notes: [this.asset.notes || '', Validators.maxLength(2000)],
    });
    this.isEditModalOpen = true;
  }

  saveEdit(): void {
    if (this.assetForm.invalid || !this.asset) return;
    const formVal = this.assetForm.value;
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

    this.assetService.update(this.assetId, updateInput).subscribe({
      next: () => {
        this.toaster.success('Đã cập nhật thông tin tài sản!');
        this.isEditModalOpen = false;
        this.loadAsset();
      }
    });
  }

  openAssignModal(): void {
    if (!this.asset) return;
    this.selectedUser = this.users.find(u => u.id === this.asset?.assignedToUserId) || null;
    this.assignForm = this.fb.group({
      userId: [this.asset.assignedToUserId || null],
      userName: [this.asset.assignedToUserName || '', [Validators.required, Validators.maxLength(128)]],
      userEmail: [this.asset.assignedToUserEmail || '', [Validators.email, Validators.maxLength(128)]],
      department: [this.asset.department || '', Validators.maxLength(128)],
      notes: ['', Validators.maxLength(2000)],
    });
    this.isAssignModalOpen = true;
  }

  saveAssign(): void {
    if (this.assignForm.invalid || !this.asset) return;
    const val = this.assignForm.value;
    const input: AssignAssetDto = {
      userId: val.userId || null,
      userName: val.userName,
      userEmail: val.userEmail || null,
      department: val.department || null,
      notes: val.notes || null,
    };

    this.assetService.assign(this.assetId, input).subscribe({
      next: () => {
        this.toaster.success(`Đã cấp phát tài sản cho ${input.userName}!`);
        this.isAssignModalOpen = false;
        this.loadAsset();
      }
    });
  }

  openReturnModal(): void {
    this.returnNotes = '';
    this.isReturnModalOpen = true;
  }

  saveReturn(): void {
    if (!this.asset) return;
    const input: ReturnAssetDto = { notes: this.returnNotes || null };
    this.assetService.return(this.assetId, input).subscribe({
      next: () => {
        this.toaster.success('Đã thu hồi thiết bị về kho!');
        this.isReturnModalOpen = false;
        this.loadAsset();
      }
    });
  }

  openStatusModal(): void {
    if (!this.asset) return;
    this.statusForm = this.fb.group({
      status: [this.asset.status, Validators.required],
      reason: ['', Validators.maxLength(1000)],
    });
    this.isStatusModalOpen = true;
  }

  saveStatus(): void {
    if (this.statusForm.invalid || !this.asset) return;
    const val = this.statusForm.value;
    const input: ChangeAssetStatusDto = {
      status: Number(val.status),
      reason: val.reason || null,
    };

    this.assetService.changeStatus(this.assetId, input).subscribe({
      next: () => {
        this.toaster.success('Đã thay đổi trạng thái tài sản!');
        this.isStatusModalOpen = false;
        this.loadAsset();
      }
    });
  }

  deleteAsset(): void {
    if (!this.asset) return;
    this.confirmation.warn(
      `Bạn có chắc chắn muốn xóa tài sản "${this.asset.name}" (${this.asset.assetTag}) không?`,
      'Xác nhận xóa tài sản'
    ).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.assetService.delete(this.assetId).subscribe({
          next: () => {
            this.toaster.success('Đã xóa tài sản thành công!');
            this.router.navigate(['/assets']);
          }
        });
      }
    });
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

  getActivityIcon(type: AssetActivityType): string {
    switch (type) {
      case AssetActivityType.Created:
        return 'fas fa-plus text-primary';
      case AssetActivityType.Assigned:
        return 'fas fa-user-check text-success';
      case AssetActivityType.Returned:
        return 'fas fa-undo text-warning';
      case AssetActivityType.StatusChanged:
        return 'fas fa-exchange-alt text-info';
      case AssetActivityType.SentToRepair:
        return 'fas fa-wrench text-danger';
      case AssetActivityType.Repaired:
        return 'fas fa-check-circle text-success';
      case AssetActivityType.TicketLinked:
        return 'fas fa-ticket-alt text-purple';
      default:
        return 'fas fa-dot-circle text-muted';
    }
  }

  async openPrintModal(): Promise<void> {
    if (!this.asset) return;
    const qrContent = `${window.location.origin}/assets/${this.asset.id}`;
    const rawSvg = await QrCodeGenerator.generateSvg(qrContent, 120);
    this.qrCodeSvg = this.sanitizer.bypassSecurityTrustHtml(rawSvg);
    this.isPrintModalOpen = true;
    this.cdr.detectChanges();
  }

  closePrintModal(): void {
    this.isPrintModalOpen = false;
  }

  printTag(): void {
    window.print();
  }

  openReceiptModal(type: 'handover' | 'return' = 'handover'): void {
    this.receiptType = type;
    this.isReceiptModalOpen = true;
  }

  closeReceiptModal(): void {
    this.isReceiptModalOpen = false;
  }
}
