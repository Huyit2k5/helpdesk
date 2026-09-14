import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { catchError } from 'rxjs/operators';
import { AssetService } from '../../../proxy/assets/asset.service';
import { CustomerPortalService } from '../../../proxy/customer-portal/customer-portal.service';
import { AssetReceiptDto } from '../../../proxy/assets/models';
import { printWithPageSize } from '../../print-page-size.helper';

@Component({
  selector: 'app-asset-receipt-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './asset-receipt-modal.component.html',
  styleUrls: ['./asset-receipt-modal.component.scss'],
})
export class AssetReceiptModalComponent implements OnChanges {
  private assetService = inject(AssetService);
  private customerPortalService = inject(CustomerPortalService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  @Input() isOpen = false;
  @Input() assetId: string | null | undefined = null;
  @Input() receiptType: 'handover' | 'return' = 'handover';
  @Output() closeModal = new EventEmitter<void>();

  receipt: AssetReceiptDto | null = null;
  isLoading = false;
  errorMessage: string | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen && this.assetId) {
      this.loadReceipt();
    }
  }

  loadReceipt(): void {
    if (!this.assetId) return;
    this.isLoading = true;
    this.errorMessage = null;

    const isPortal = this.router.url.includes('/portal');
    const request$ = isPortal
      ? this.customerPortalService.getMyAssetReceipt(this.assetId, this.receiptType)
      : this.assetService.getReceipt(this.assetId, this.receiptType).pipe(
          catchError((err) => {
            if (err.status === 403 && this.assetId) {
              return this.customerPortalService.getMyAssetReceipt(this.assetId, this.receiptType);
            }
            throw err;
          })
        );

    request$.subscribe({
      next: (res: any) => {
        this.receipt = res;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error('Error loading receipt:', err);
        this.errorMessage = 'Không thể tải thông tin biên bản. Vui lòng thử lại sau.';
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  printReceipt(): void {
    printWithPageSize('size: A4 portrait; margin: 15mm;');
  }

  onClose(): void {
    this.closeModal.emit();
  }
}
