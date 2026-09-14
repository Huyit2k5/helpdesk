import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { CustomerPortalService } from '../../proxy/customer-portal/customer-portal.service';
import { AssetDto, AssetStatus, AssetType } from '../../proxy/assets/models';
import { ToasterService } from '@abp/ng.theme.shared';

import { AssetReceiptModalComponent } from '../../assets/components/asset-receipt-modal/asset-receipt-modal.component';

@Component({
  selector: 'app-my-assets',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, AssetReceiptModalComponent],
  templateUrl: './my-assets.component.html',
  styleUrls: ['./my-assets.component.scss'],
})
export class MyAssetsComponent implements OnInit {
  private customerPortalService = inject(CustomerPortalService);
  private router = inject(Router);
  private toaster = inject(ToasterService);
  private cdr = inject(ChangeDetectorRef);

  assets: AssetDto[] = [];
  filteredAssets: AssetDto[] = [];
  isLoading = true;
  filterText = '';

  selectedAssetForDetail: AssetDto | null = null;
  isDetailModalOpen = false;

  // Receipt & Signoff Modal
  isReceiptModalOpen = false;
  selectedReceiptAssetId: string | null = null;

  isSignoffModalOpen = false;
  selectedAssetForSignoff: AssetDto | null = null;
  signoffNotes = '';
  signoffTermsAccepted = false;
  isSigning = false;

  AssetStatus = AssetStatus;
  AssetType = AssetType;

  ngOnInit(): void {
    this.loadMyAssets();
  }

  loadMyAssets(): void {
    this.isLoading = true;
    this.customerPortalService.getMyAssets().subscribe({
      next: (res) => {
        this.assets = res || [];
        this.applyFilter();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.toaster.error(err?.error?.message || 'Không thể tải danh sách thiết bị', 'Lỗi');
        this.cdr.detectChanges();
      },
    });
  }

  applyFilter(): void {
    if (!this.filterText.trim()) {
      this.filteredAssets = [...this.assets];
    } else {
      const q = this.filterText.toLowerCase().trim();
      this.filteredAssets = this.assets.filter(
        (a) =>
          a.assetTag?.toLowerCase().includes(q) ||
          a.name?.toLowerCase().includes(q) ||
          a.model?.toLowerCase().includes(q) ||
          a.serialNumber?.toLowerCase().includes(q) ||
          a.manufacturer?.toLowerCase().includes(q)
      );
    }
  }

  reportIssue(asset: AssetDto): void {
    this.router.navigate(['/portal/my-tickets'], {
      queryParams: {
        create: 'true',
        assetId: asset.id,
        assetTag: asset.assetTag,
        assetName: asset.name,
      },
    });
  }

  openDetailModal(asset: AssetDto): void {
    this.selectedAssetForDetail = asset;
    this.isDetailModalOpen = true;
  }

  closeDetailModal(): void {
    this.selectedAssetForDetail = null;
    this.isDetailModalOpen = false;
  }

  copyToClipboard(text?: string | null, label: string = 'Mã'): void {
    if (!text) return;
    navigator.clipboard.writeText(text).then(() => {
      this.toaster.info(`Đã sao chép ${label}: ${text}`, 'Thành công');
    });
  }

  getAssetIcon(type: AssetType): string {
    switch (type) {
      case AssetType.Laptop:
        return 'fas fa-laptop text-primary';
      case AssetType.Desktop:
        return 'fas fa-desktop text-info';
      case AssetType.Monitor:
        return 'fas fa-tv text-warning';
      case AssetType.NetworkDevice:
        return 'fas fa-network-wired text-success';
      case AssetType.PrinterPeripheral:
        return 'fas fa-print text-secondary';
      case AssetType.ServerStorage:
        return 'fas fa-server text-danger';
      case AssetType.SoftwareLicense:
        return 'fas fa-key text-purple';
      case AssetType.MobileDevice:
        return 'fas fa-mobile-alt text-teal';
      default:
        return 'fas fa-cube text-secondary';
    }
  }

  getStatusBadgeClass(status: AssetStatus): string {
    switch (status) {
      case AssetStatus.Assigned:
        return 'badge bg-success-subtle text-success border border-success-subtle';
      case AssetStatus.UnderRepair:
        return 'badge bg-warning-subtle text-warning border border-warning-subtle';
      case AssetStatus.InStock:
        return 'badge bg-info-subtle text-info border border-info-subtle';
      case AssetStatus.Reserved:
        return 'badge bg-primary-subtle text-primary border border-primary-subtle';
      case AssetStatus.Retired:
        return 'badge bg-secondary-subtle text-secondary border border-secondary-subtle';
      default:
        return 'badge bg-light text-dark border';
    }
  }

  isWarrantyActive(warrantyExpiryDate?: string): boolean {
    if (!warrantyExpiryDate) return false;
    return new Date(warrantyExpiryDate) >= new Date();
  }

  openSignoffModal(asset: AssetDto): void {
    this.selectedAssetForSignoff = asset;
    this.signoffNotes = '';
    this.signoffTermsAccepted = false;
    this.isSignoffModalOpen = true;
  }

  closeSignoffModal(): void {
    this.selectedAssetForSignoff = null;
    this.isSignoffModalOpen = false;
  }

  confirmSignoff(): void {
    if (!this.selectedAssetForSignoff || !this.selectedAssetForSignoff.id || !this.signoffTermsAccepted) return;
    const assetId = this.selectedAssetForSignoff.id;
    this.isSigning = true;

    this.customerPortalService.confirmAssetHandover(assetId, {
      notes: this.signoffNotes || 'Nhân sự đã xác nhận nhận đủ thiết bị trực tuyến'
    }).subscribe({
      next: () => {
        this.isSigning = false;
        this.toaster.success('Ký nhận bàn giao thiết bị thành công!', 'Hoàn tất');
        this.closeSignoffModal();
        this.loadMyAssets();
      },
      error: (err) => {
        this.isSigning = false;
        this.toaster.error(err?.error?.message || 'Không thể ký nhận bàn giao lúc này', 'Lỗi');
        this.cdr.detectChanges();
      }
    });
  }

  openReceiptModal(assetId?: string): void {
    if (!assetId) return;
    this.selectedReceiptAssetId = assetId;
    this.isReceiptModalOpen = true;
  }

  closeReceiptModal(): void {
    this.isReceiptModalOpen = false;
    this.selectedReceiptAssetId = null;
  }
}
