import { ChangeDetectorRef, Component, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToasterService } from '@abp/ng.theme.shared';
import { DiscordAccountLinkService } from '../proxy/discord-account-link/discord-account-link.service';

@Component({
  selector: 'app-discord-link',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './discord-link.component.html',
  styleUrls: ['./discord-link.component.scss'],
})
export class DiscordLinkComponent implements OnDestroy {
  private linkService = inject(DiscordAccountLinkService);
  private toaster = inject(ToasterService);
  private cdr = inject(ChangeDetectorRef);

  isGenerating = false;
  code: string | null = null;
  expiresAtUtc: string | null = null;
  remainingSeconds = 0;

  private countdownHandle?: ReturnType<typeof setInterval>;

  generateCode(): void {
    this.isGenerating = true;
    this.cdr.detectChanges();

    this.linkService.generateLinkCode().subscribe({
      next: (result) => {
        this.code = result.code;
        this.expiresAtUtc = result.expiresAtUtc;
        this.isGenerating = false;
        this.startCountdown();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isGenerating = false;
        this.toaster.error('Không thể tạo mã liên kết: ' + (err?.error?.message || err.message || 'Lỗi server'));
        this.cdr.detectChanges();
      },
    });
  }

  copyCode(): void {
    if (!this.code) {
      return;
    }
    navigator.clipboard?.writeText(this.code).then(
      () => this.toaster.success('Đã sao chép mã liên kết vào clipboard.', 'Thành công'),
      () => this.toaster.warn('Không thể sao chép tự động, vui lòng chọn và sao chép thủ công.'),
    );
  }

  private startCountdown(): void {
    this.stopCountdown();
    if (!this.expiresAtUtc) {
      return;
    }

    const expiresAtMs = new Date(this.expiresAtUtc).getTime();
    const tick = () => {
      const diffSeconds = Math.floor((expiresAtMs - Date.now()) / 1000);
      this.remainingSeconds = Math.max(diffSeconds, 0);
      if (this.remainingSeconds <= 0) {
        this.code = null;
        this.expiresAtUtc = null;
        this.stopCountdown();
      }
      this.cdr.detectChanges();
    };

    tick();
    this.countdownHandle = setInterval(tick, 1000);
  }

  private stopCountdown(): void {
    if (this.countdownHandle) {
      clearInterval(this.countdownHandle);
      this.countdownHandle = undefined;
    }
  }

  ngOnDestroy(): void {
    this.stopCountdown();
  }
}
