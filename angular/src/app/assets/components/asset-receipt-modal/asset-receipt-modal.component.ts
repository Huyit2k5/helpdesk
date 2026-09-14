import { Component, Input, Output, EventEmitter, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AssetService } from '../../../proxy/assets/asset.service';
import { AssetReceiptDto } from '../../../proxy/assets/models';

@Component({
  selector: 'app-asset-receipt-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './asset-receipt-modal.component.html',
  styleUrls: ['./asset-receipt-modal.component.scss'],
})
export class AssetReceiptModalComponent implements OnChanges {
  private assetService = inject(AssetService);

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

    this.assetService.getReceipt(this.assetId, this.receiptType).subscribe({
      next: (res: any) => {
        this.receipt = res;
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Error loading receipt:', err);
        this.errorMessage = 'Không thể tải thông tin biên bản. Vui lòng thử lại sau.';
        this.isLoading = false;
      }
    });
  }

  printReceipt(): void {
    window.print();
  }

  onClose(): void {
    this.closeModal.emit();
  }
}
