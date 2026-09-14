import { Component, Input, Output, EventEmitter, inject, ViewChild, ElementRef, OnChanges, SimpleChanges, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AssetService } from '../../../proxy/assets/asset.service';
import { AssetDto, AssetStatus, AssetType } from '../../../proxy/assets/models';

@Component({
  selector: 'app-qr-scanner-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './qr-scanner-modal.component.html',
  styleUrls: ['./qr-scanner-modal.component.scss'],
})
export class QrScannerModalComponent implements OnChanges, OnDestroy {
  private assetService = inject(AssetService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  @Input() isOpen = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() openReceipt = new EventEmitter<{ assetId: string; type: string }>();

  @ViewChild('videoElement') videoElement?: ElementRef<HTMLVideoElement>;
  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  scanMode: 'camera' | 'upload' | 'manual' = 'camera';
  mediaStream: MediaStream | null = null;
  scanningAnimation = true;
  scanIntervalId: any = null;

  isSearching = false;
  detectedCode: string | null = null;
  scannedAsset: AssetDto | null = null;
  searchError: string | null = null;
  manualCode = '';
  isCameraSupported = true;

  AssetStatus = AssetStatus;
  AssetType = AssetType;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        this.resetState();
        setTimeout(() => this.initCamera(), 300);
      } else {
        this.stopCamera();
      }
    }
  }

  ngOnDestroy(): void {
    this.stopCamera();
  }

  resetState(): void {
    this.scannedAsset = null;
    this.detectedCode = null;
    this.searchError = null;
    this.manualCode = '';
  }

  setMode(mode: 'camera' | 'upload' | 'manual'): void {
    this.scanMode = mode;
    this.resetState();
    if (mode === 'camera') {
      this.initCamera();
    } else {
      this.stopCamera();
    }
  }

  async initCamera(): Promise<void> {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      this.isCameraSupported = false;
      this.scanMode = 'manual';
      return;
    }

    try {
      this.stopCamera();
      this.mediaStream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: { ideal: 'environment' } }
      });

      if (this.videoElement && this.videoElement.nativeElement) {
        this.videoElement.nativeElement.srcObject = this.mediaStream;
        this.videoElement.nativeElement.play();
        this.startDetectionLoop();
      }
    } catch (err) {
      console.warn('Camera access denied or unavailable:', err);
      this.isCameraSupported = false;
      this.scanMode = 'manual';
      this.cdr.detectChanges();
    }
  }

  stopCamera(): void {
    if (this.scanIntervalId) {
      clearInterval(this.scanIntervalId);
      this.scanIntervalId = null;
    }
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop());
      this.mediaStream = null;
    }
  }

  startDetectionLoop(): void {
    if (this.scanIntervalId) clearInterval(this.scanIntervalId);

    const hasBarcodeDetector = 'BarcodeDetector' in window;

    this.scanIntervalId = setInterval(async () => {
      if (!this.isOpen || this.scanMode !== 'camera' || !this.videoElement || this.isSearching || this.scannedAsset) {
        return;
      }

      const video = this.videoElement.nativeElement;
      if (video.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) return;

      if (hasBarcodeDetector) {
        try {
          const barcodeDetector = new (window as any).BarcodeDetector({ formats: ['qr_code', 'code_128', 'code_39'] });
          const barcodes = await barcodeDetector.detect(video);
          if (barcodes && barcodes.length > 0) {
            const rawValue = barcodes[0].rawValue;
            this.handleDetectedCode(rawValue);
          }
        } catch (e) {
          // ignore detection frame errors
        }
      }
    }, 400);
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isSearching = true;
    this.searchError = null;

    try {
      if ('BarcodeDetector' in window) {
        const imageBitmap = await createImageBitmap(file);
        const barcodeDetector = new (window as any).BarcodeDetector({ formats: ['qr_code', 'code_128', 'code_39'] });
        const barcodes = await barcodeDetector.detect(imageBitmap);
        if (barcodes && barcodes.length > 0) {
          this.handleDetectedCode(barcodes[0].rawValue);
        } else {
          this.searchError = 'Không nhận diện được mã QR trong hình ảnh tải lên. Vui lòng thử ảnh rõ nét hơn hoặc nhập mã trực tiếp.';
          this.isSearching = false;
          this.cdr.detectChanges();
        }
      } else {
        // Fallback: prompt filename if it contains tag
        const match = file.name.match(/AST-\d{8}-\d{4}/i);
        if (match) {
          this.handleDetectedCode(match[0]);
        } else {
          this.searchError = 'Trình duyệt hiện tại chưa hỗ trợ quét ảnh QR trực tiếp. Vui lòng chuyển sang tab Nhập mã thủ công.';
          this.isSearching = false;
          this.cdr.detectChanges();
        }
      }
    } catch (err) {
      console.error('File scan error:', err);
      this.searchError = 'Không thể phân tích ảnh mã QR. Vui lòng kiểm tra lại.';
      this.isSearching = false;
      this.cdr.detectChanges();
    }
  }

  handleDetectedCode(rawText: string): void {
    if (!rawText) return;
    this.detectedCode = rawText.trim();

    // Extract AST tag from potential URL or JSON or raw string
    let assetTag = this.detectedCode;
    const tagMatch = this.detectedCode.match(/AST-\d{8}-\d{4}/i);
    if (tagMatch) {
      assetTag = tagMatch[0];
    }

    this.lookupAsset(assetTag);
  }

  lookupAsset(tagOrSerial: string): void {
    if (!tagOrSerial || !tagOrSerial.trim()) return;
    const cleanQuery = tagOrSerial.trim();

    this.isSearching = true;
    this.searchError = null;
    this.scannedAsset = null;

    this.assetService.getByAssetTag(cleanQuery).subscribe({
      next: (asset) => {
        this.isSearching = false;
        if (asset && asset.id) {
          this.scannedAsset = asset;
          this.stopCamera();
        } else {
          this.searchError = `Không tìm thấy thiết bị nào có mã hoặc Serial khớp với "${cleanQuery}".`;
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isSearching = false;
        this.searchError = `Lỗi tra cứu thiết bị: ${err?.error?.message || 'Không tìm thấy dữ liệu'}`;
        this.cdr.detectChanges();
      }
    });
  }

  scanAgain(): void {
    this.resetState();
    if (this.scanMode === 'camera') {
      this.initCamera();
    }
  }

  viewAssetDetail(asset: AssetDto): void {
    this.close();
    this.router.navigate(['/assets/detail', asset.id]);
  }

  printReceiptForAsset(asset: AssetDto): void {
    if (asset && asset.id) {
      this.openReceipt.emit({ assetId: asset.id, type: 'handover' });
    }
  }

  close(): void {
    this.stopCamera();
    this.closeModal.emit();
  }
}
