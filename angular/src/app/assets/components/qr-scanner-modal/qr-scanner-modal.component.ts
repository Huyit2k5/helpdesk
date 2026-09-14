import { Component, Input, Output, EventEmitter, inject, ViewChild, ElementRef, OnChanges, SimpleChanges, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import jsQR from 'jsqr';
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

  // Canvas ẩn dùng chung để lấy ImageData cho jsQR (fallback khi BarcodeDetector
  // không khả dụng trên trình duyệt hiện tại - Firefox/Safari).
  private decodeCanvas: HTMLCanvasElement | null = null;

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
      } else {
        // Fallback cho trình duyệt không có BarcodeDetector (Firefox/Safari):
        // vẽ frame video hiện tại lên canvas ẩn rồi giải mã bằng jsQR.
        try {
          const imageData = this.captureFrameAsImageData(video, video.videoWidth, video.videoHeight);
          const decoded = imageData ? this.decodeQrFromImageData(imageData) : null;
          if (decoded) {
            this.handleDetectedCode(decoded);
          }
        } catch (e) {
          // ignore detection frame errors
        }
      }
    }, 400);
  }

  /**
   * Vẽ 1 nguồn ảnh (video hiện tại hoặc ảnh upload) lên canvas ẩn dùng chung và
   * trả về ImageData để đưa vào jsQR giải mã.
   */
  private captureFrameAsImageData(
    source: CanvasImageSource,
    width: number,
    height: number
  ): ImageData | null {
    if (!width || !height) return null;

    if (!this.decodeCanvas) {
      this.decodeCanvas = document.createElement('canvas');
    }
    this.decodeCanvas.width = width;
    this.decodeCanvas.height = height;

    const ctx = this.decodeCanvas.getContext('2d', { willReadFrequently: true });
    if (!ctx) return null;

    ctx.drawImage(source, 0, 0, width, height);
    return ctx.getImageData(0, 0, width, height);
  }

  /** Giải mã QR thuần JS bằng jsQR - hoạt động trên mọi trình duyệt, không cần BarcodeDetector. */
  private decodeQrFromImageData(imageData: ImageData): string | null {
    const result = jsQR(imageData.data, imageData.width, imageData.height, {
      inversionAttempts: 'attemptBoth',
    });
    return result?.data ?? null;
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isSearching = true;
    this.searchError = null;

    try {
      const imageBitmap = await createImageBitmap(file);
      let decodedText: string | null = null;

      // Đường nhanh: dùng BarcodeDetector của trình duyệt nếu có (Chrome/Edge).
      if ('BarcodeDetector' in window) {
        try {
          const barcodeDetector = new (window as any).BarcodeDetector({ formats: ['qr_code', 'code_128', 'code_39'] });
          const barcodes = await barcodeDetector.detect(imageBitmap);
          if (barcodes && barcodes.length > 0) {
            decodedText = barcodes[0].rawValue;
          }
        } catch {
          // BarcodeDetector tồn tại nhưng detect() lỗi (thiếu module barcode nền tảng) - rơi xuống jsQR bên dưới.
        }
      }

      // Giải mã thật bằng jsQR - hoạt động trên MỌI trình duyệt (Firefox/Safari không có BarcodeDetector).
      if (!decodedText) {
        const imageData = this.captureFrameAsImageData(imageBitmap, imageBitmap.width, imageBitmap.height);
        decodedText = imageData ? this.decodeQrFromImageData(imageData) : null;
      }

      if (decodedText) {
        this.handleDetectedCode(decodedText);
      } else {
        this.searchError = 'Không nhận diện được mã QR trong hình ảnh tải lên. Vui lòng thử ảnh rõ nét hơn hoặc nhập mã trực tiếp.';
        this.isSearching = false;
        this.cdr.detectChanges();
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

    // 1) Tem QR do chính hệ thống in ra mã hoá URL nội bộ dạng {origin}/assets/{guid}
    //    (xem asset-detail.component.ts:openPrintModal) - tra thẳng theo Id là chính xác
    //    tuyệt đối, không phụ thuộc định dạng AssetTag.
    const urlMatch = this.detectedCode.match(
      /\/assets\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})/
    );
    if (urlMatch) {
      this.lookupAssetById(urlMatch[1]);
      return;
    }

    // 2) Mã thẻ chuẩn AST-yyyyMMdd-#### xuất hiện đâu đó trong chuỗi quét được.
    const tagMatch = this.detectedCode.match(/AST-\d{8}-\d{4}/i);
    const assetTag = tagMatch ? tagMatch[0] : this.detectedCode;

    // 3) Súng quét mã vạch Serial/Code128, hoặc fallback: tra theo tag/serial như cũ.
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

  /** Tra cứu chính xác theo Id (GUID) - dùng khi quét được QR tem do hệ thống tự in ra. */
  lookupAssetById(id: string): void {
    this.isSearching = true;
    this.searchError = null;
    this.scannedAsset = null;

    this.assetService.get(id).subscribe({
      next: (asset) => {
        this.isSearching = false;
        if (asset && asset.id) {
          this.scannedAsset = asset;
          this.stopCamera();
        } else {
          this.searchError = 'Không tìm thấy thiết bị tương ứng với mã QR này.';
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
    this.router.navigate(['/assets', asset.id]);
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
