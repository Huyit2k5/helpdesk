import { Component, OnInit, OnDestroy, inject, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import jsQR from 'jsqr';
import { AssetAuditService } from '../../../proxy/assets/asset-audit.service';
import {
  AssetAuditSessionDto,
  AssetAuditItemDto,
  AssetAuditStatus,
  AuditItemResult,
  ScanResultDto,
  AuditReportDto,
} from '../../../proxy/assets/models';

@Component({
  selector: 'app-asset-audit-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './asset-audit-detail.component.html',
  styleUrls: ['./asset-audit-detail.component.scss'],
})
export class AssetAuditDetailComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private auditService = inject(AssetAuditService);
  private toaster = inject(ToasterService);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);

  sessionId!: string;
  session: AssetAuditSessionDto | null = null;
  loading = true;
  activeFilterTab: 'all' | 'matched' | 'displaced' | 'missing' | 'unexpected' = 'all';

  // Scanner Cockpit State
  scannerMode: 'camera' | 'barcode-gun' = 'barcode-gun';
  isCameraRunning = false;
  isSoundEnabled = true;
  mediaStream: MediaStream | null = null;
  scanIntervalId: any = null;
  lastScannedCode: string | null = null;
  lastScanTimestamp: number = 0;
  scanCooldownMs: number = 1800; // avoid repeated triggers on same code in camera mode

  @ViewChild('videoElement') videoElement?: ElementRef<HTMLVideoElement>;
  @ViewChild('barcodeInput') barcodeInput?: ElementRef<HTMLInputElement>;
  private decodeCanvas: HTMLCanvasElement | null = null;

  // Manual / Barcode Gun input
  barcodeInputValue: string = '';
  currentScanLocation: string = '';
  isProcessingScan = false;
  lastScanResult: ScanResultDto | null = null;

  // Report Modal
  isReportModalOpen = false;
  reportData: AuditReportDto | null = null;
  loadingReport = false;

  // Reconciliation state
  isReconciling = false;

  // Enums
  AssetAuditStatus = AssetAuditStatus;
  AuditItemResult = AuditItemResult;

  // Audio Context for beeps
  private audioCtx: AudioContext | null = null;

  ngOnInit(): void {
    this.sessionId = this.route.snapshot.paramMap.get('id') || '';
    if (!this.sessionId) {
      this.router.navigate(['/assets/audits']);
      return;
    }
    this.loadSessionDetails();
  }

  ngOnDestroy(): void {
    this.stopCamera();
    if (this.audioCtx && this.audioCtx.state !== 'closed') {
      try {
        this.audioCtx.close();
      } catch {}
    }
  }

  loadSessionDetails(callback?: () => void): void {
    this.loading = true;
    this.auditService.get(this.sessionId).subscribe({
      next: res => {
        this.session = res;
        this.loading = false;
        if (!this.currentScanLocation && res.scopeLocation) {
          this.currentScanLocation = res.scopeLocation;
        }
        if (callback) callback();
        this.cdr.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.toaster.error('Không tìm thấy đợt kiểm kê này.');
        this.router.navigate(['/assets/audits']);
      },
    });
  }

  get filteredItems(): AssetAuditItemDto[] {
    if (!this.session?.items) return [];
    switch (this.activeFilterTab) {
      case 'matched':
        return this.session.items.filter(x => x.resultStatus === AuditItemResult.Matched);
      case 'displaced':
        return this.session.items.filter(x => x.resultStatus === AuditItemResult.Displaced);
      case 'missing':
        return this.session.items.filter(x => x.resultStatus === AuditItemResult.Pending);
      case 'unexpected':
        return this.session.items.filter(x => x.resultStatus === AuditItemResult.Unexpected);
      default:
        return this.session.items;
    }
  }

  get unexpectedCount(): number {
    return this.session?.items.filter(x => x.resultStatus === AuditItemResult.Unexpected).length || 0;
  }

  get unreconciledDisplacedCount(): number {
    return (
      this.session?.items.filter(
        x => (x.resultStatus === AuditItemResult.Displaced || x.resultStatus === AuditItemResult.Unexpected) && !x.isReconciled
      ).length || 0
    );
  }

  // Scanner Mode switching
  setScannerMode(mode: 'camera' | 'barcode-gun'): void {
    this.scannerMode = mode;
    if (mode === 'camera') {
      setTimeout(() => this.startCamera(), 200);
    } else {
      this.stopCamera();
      setTimeout(() => this.focusBarcodeInput(), 200);
    }
  }

  focusBarcodeInput(): void {
    if (this.barcodeInput?.nativeElement) {
      this.barcodeInput.nativeElement.focus();
    }
  }

  toggleSound(): void {
    this.isSoundEnabled = !this.isSoundEnabled;
  }

  // Audio Beep generator using Web Audio API
  private playBeep(type: 'success' | 'warning' | 'error'): void {
    if (!this.isSoundEnabled) return;

    try {
      if (!this.audioCtx) {
        const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
        this.audioCtx = new AudioContextClass();
      }

      if (this.audioCtx.state === 'suspended') {
        this.audioCtx.resume();
      }

      const now = this.audioCtx.currentTime;
      const osc = this.audioCtx.createOscillator();
      const gain = this.audioCtx.createGain();

      osc.connect(gain);
      gain.connect(this.audioCtx.destination);

      if (type === 'success') {
        // High pleasant double-beep: 880Hz -> 1320Hz
        osc.type = 'sine';
        osc.frequency.setValueAtTime(880, now);
        osc.frequency.exponentialRampToValueAtTime(1320, now + 0.12);
        gain.gain.setValueAtTime(0.2, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.15);
        osc.start(now);
        osc.stop(now + 0.15);
      } else if (type === 'warning') {
        // Warning chime: 440Hz -> 330Hz
        osc.type = 'sawtooth';
        osc.frequency.setValueAtTime(550, now);
        osc.frequency.linearRampToValueAtTime(330, now + 0.25);
        gain.gain.setValueAtTime(0.25, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.3);
        osc.start(now);
        osc.stop(now + 0.3);
      } else {
        // Error buzzer: 220Hz
        osc.type = 'square';
        osc.frequency.setValueAtTime(220, now);
        gain.gain.setValueAtTime(0.2, now);
        gain.gain.exponentialRampToValueAtTime(0.01, now + 0.35);
        osc.start(now);
        osc.stop(now + 0.35);
      }
    } catch {}
  }

  // Camera Management
  async startCamera(): Promise<void> {
    if (this.isCameraRunning) return;

    try {
      const constraints: MediaStreamConstraints = {
        video: {
          facingMode: { ideal: 'environment' },
          width: { ideal: 1280 },
          height: { ideal: 720 },
        },
      };

      this.mediaStream = await navigator.mediaDevices.getUserMedia(constraints);
      this.isCameraRunning = true;

      setTimeout(() => {
        if (this.videoElement?.nativeElement) {
          this.videoElement.nativeElement.srcObject = this.mediaStream;
          this.videoElement.nativeElement.play();
          this.startContinuousScanLoop();
        }
      }, 300);
    } catch (err) {
      this.isCameraRunning = false;
      this.toaster.error('Không thể truy cập camera. Vui lòng cấp quyền hoặc sử dụng chế độ Nhập Súng Quét.');
    }
  }

  stopCamera(): void {
    if (this.scanIntervalId) {
      clearInterval(this.scanIntervalId);
      this.scanIntervalId = null;
    }
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(t => t.stop());
      this.mediaStream = null;
    }
    this.isCameraRunning = false;
  }

  private startContinuousScanLoop(): void {
    if (this.scanIntervalId) {
      clearInterval(this.scanIntervalId);
    }

    this.scanIntervalId = setInterval(async () => {
      if (!this.isCameraRunning || this.isProcessingScan) return;
      if (!this.videoElement?.nativeElement) return;

      const video = this.videoElement.nativeElement;
      if (video.readyState !== video.HAVE_ENOUGH_DATA) return;

      let detectedCode: string | null = null;

      // 1. Try BarcodeDetector if available
      if ('BarcodeDetector' in window) {
        try {
          const detector = new (window as any).BarcodeDetector({
            formats: ['qr_code', 'code_128', 'code_39', 'ean_13'],
          });
          const barcodes = await detector.detect(video);
          if (barcodes && barcodes.length > 0) {
            detectedCode = barcodes[0].rawValue;
          }
        } catch {}
      }

      // 2. Fallback to jsQR
      if (!detectedCode) {
        const width = video.videoWidth;
        const height = video.videoHeight;
        if (width && height) {
          if (!this.decodeCanvas) {
            this.decodeCanvas = document.createElement('canvas');
          }
          this.decodeCanvas.width = width;
          this.decodeCanvas.height = height;
          const ctx = this.decodeCanvas.getContext('2d', { willReadFrequently: true });
          if (ctx) {
            ctx.drawImage(video, 0, 0, width, height);
            const imgData = ctx.getImageData(0, 0, width, height);
            const qrResult = jsQR(imgData.data, imgData.width, imgData.height, {
              inversionAttempts: 'attemptBoth',
            });
            if (qrResult) {
              detectedCode = qrResult.data;
            }
          }
        }
      }

      if (detectedCode) {
        const now = Date.now();
        // Cooldown check for the same code
        if (detectedCode === this.lastScannedCode && now - this.lastScanTimestamp < this.scanCooldownMs) {
          return;
        }

        this.lastScannedCode = detectedCode;
        this.lastScanTimestamp = now;
        this.executeScan(detectedCode);
      }
    }, 350);
  }

  // Handle Manual / Barcode Gun input
  onBarcodeInputSubmit(): void {
    const raw = (this.barcodeInputValue || '').trim();
    if (!raw) return;

    this.executeScan(raw, () => {
      this.barcodeInputValue = '';
      this.focusBarcodeInput();
    });
  }

  // Core scan execution method
  executeScan(code: string, onComplete?: () => void): void {
    if (this.isProcessingScan) return;
    this.isProcessingScan = true;

    this.auditService
      .scanItem(this.sessionId, {
        code: code,
        currentLocation: this.currentScanLocation || null,
      })
      .subscribe({
        next: result => {
          this.isProcessingScan = false;
          this.lastScanResult = result;

          if (result.success) {
            // Update counts in local session object
            if (result.updatedCounts && this.session) {
              this.session.totalExpectedCount = result.updatedCounts.totalExpectedCount;
              this.session.scannedCount = result.updatedCounts.scannedCount;
              this.session.matchedCount = result.updatedCounts.matchedCount;
              this.session.displacedCount = result.updatedCounts.displacedCount;
              this.session.missingCount = result.updatedCounts.missingCount;
              this.session.progressPercentage = result.updatedCounts.progressPercentage;
            }

            // Sound feedback
            if (result.resultStatus === AuditItemResult.Matched) {
              this.playBeep('success');
            } else if (result.resultStatus === AuditItemResult.Displaced) {
              this.playBeep('warning');
            } else {
              this.playBeep('warning');
            }

            // Update item in local list or reload
            this.loadSessionDetails();
          } else {
            this.playBeep('error');
            this.toaster.warn(result.message || 'Mã không khớp với thiết bị nào trong hệ thống.');
          }

          if (onComplete) onComplete();
          this.cdr.markForCheck();
        },
        error: err => {
          this.isProcessingScan = false;
          this.playBeep('error');
          this.toaster.error(err?.error?.message || 'Lỗi khi gửi yêu cầu quét kiểm kê.');
          if (onComplete) onComplete();
          this.cdr.markForCheck();
        },
      });
  }

  // Reconciliation Action
  reconcileAll(): void {
    if (this.unreconciledDisplacedCount === 0) {
      this.toaster.info('Hiện không có thiết bị lệch nào cần cập nhật hồ sơ.');
      return;
    }

    this.confirmation
      .warn(
        `Áp dụng đối soát sẽ cập nhật vị trí thực tế của ${this.unreconciledDisplacedCount} thiết bị vào hệ thống và ghi nhận kiểm toán. Bạn có chắc chắn?`,
        'Xác nhận đối soát & chuẩn hóa'
      )
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.isReconciling = true;
          this.auditService.reconcile(this.sessionId, {}).subscribe({
            next: updated => {
              this.isReconciling = false;
              this.session = updated;
              this.toaster.success('Đã cập nhật vị trí thực tế cho toàn bộ thiết bị lệch thành công!');
              this.loadSessionDetails();
              this.cdr.markForCheck();
            },
            error: () => {
              this.isReconciling = false;
              this.toaster.error('Không thể thực hiện đối soát.');
              this.cdr.markForCheck();
            },
          });
        }
      });
  }

  // Session State Actions
  startAudit(): void {
    this.auditService.start(this.sessionId).subscribe({
      next: res => {
        this.session = res;
        this.toaster.success(`Đợt kiểm kê [${res.auditCode}] đã chuyển sang trạng thái Đang Kiểm Kê!`);
        this.cdr.markForCheck();
      },
      error: () => this.toaster.error('Không thể bắt đầu đợt kiểm kê.'),
    });
  }

  completeAudit(): void {
    const unScanned = this.session?.missingCount || 0;
    const msg =
      unScanned > 0
        ? `Vẫn còn ${unScanned} thiết bị chưa quét thấy (thiếu). Bạn có chắc chắn muốn hoàn tất và chốt đợt kiểm kê này?`
        : `Xác nhận chốt và hoàn tất đợt kiểm kê [${this.session?.auditCode}]?`;

    this.confirmation.warn(msg, 'Hoàn tất đợt kiểm kê').subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.auditService.complete(this.sessionId).subscribe({
          next: res => {
            this.session = res;
            this.toaster.success('Đợt kiểm kê đã được chốt và hoàn tất thành công!');
            this.openReportModal();
            this.cdr.markForCheck();
          },
          error: () => this.toaster.error('Không thể hoàn tất đợt kiểm kê.'),
        });
      }
    });
  }

  // A4 Report Modal & Print
  openReportModal(): void {
    this.loadingReport = true;
    this.isReportModalOpen = true;
    this.auditService.getReport(this.sessionId).subscribe({
      next: report => {
        this.reportData = report;
        this.loadingReport = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.loadingReport = false;
        this.toaster.error('Không thể tạo báo cáo kiểm kê.');
        this.closeReportModal();
      },
    });
  }

  closeReportModal(): void {
    this.isReportModalOpen = false;
    this.reportData = null;
  }

  printReport(): void {
    window.print();
  }

  getResultBadgeClass(result: AuditItemResult): string {
    switch (result) {
      case AuditItemResult.Matched:
        return 'badge-subtle-success';
      case AuditItemResult.Displaced:
        return 'badge-subtle-warning';
      case AuditItemResult.Unexpected:
        return 'badge-subtle-danger';
      case AuditItemResult.Pending:
      default:
        return 'badge-subtle-secondary';
    }
  }
}
