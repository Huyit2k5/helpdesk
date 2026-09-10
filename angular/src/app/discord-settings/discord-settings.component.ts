import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { DiscordSettingsService } from '../proxy/discord-settings/discord-settings.service';
import { DiscordSettingsDto, UpdateDiscordSettingsDto } from '../proxy/discord-settings/models';

@Component({
  selector: 'app-discord-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './discord-settings.component.html',
  styleUrls: ['./discord-settings.component.scss'],
})
export class DiscordSettingsComponent implements OnInit {
  private discordService = inject(DiscordSettingsService);
  private toaster = inject(ToasterService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  form!: FormGroup;
  isLoading = true;
  isSaving = false;
  isTesting = false;

  defaultBotName = 'Helpdesk Support Bot';
  defaultAvatarUrl = 'https://cdn-icons-png.flaticon.com/512/4712/4712035.png';

  ngOnInit(): void {
    this.buildForm();
    this.loadSettings();
  }

  buildForm(): void {
    this.form = this.fb.group({
      webhookUrl: ['', [Validators.maxLength(500)]],
      isEnabled: [false],
      notifyOnNewTicket: [true],
      notifyOnCriticalOnly: [false],
      notifyOnAssigned: [true],
      notifyOnSlaBreach: [true],
      notifyOnResolved: [true],
      botName: [this.defaultBotName, [Validators.maxLength(100)]],
      avatarUrl: [this.defaultAvatarUrl, [Validators.maxLength(500)]],
    });
  }

  loadSettings(): void {
    this.isLoading = true;
    this.cdr.detectChanges();

    this.discordService.get().subscribe({
      next: (settings: DiscordSettingsDto) => {
        if (settings) {
          this.form.patchValue({
            webhookUrl: settings.webhookUrl || '',
            isEnabled: settings.isEnabled,
            notifyOnNewTicket: settings.notifyOnNewTicket,
            notifyOnCriticalOnly: settings.notifyOnCriticalOnly,
            notifyOnAssigned: settings.notifyOnAssigned,
            notifyOnSlaBreach: settings.notifyOnSlaBreach,
            notifyOnResolved: settings.notifyOnResolved,
            botName: settings.botName || this.defaultBotName,
            avatarUrl: settings.avatarUrl || this.defaultAvatarUrl,
          });
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toaster.error('Không thể tải cấu hình Discord: ' + (err.message || 'Lỗi kết nối'));
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  save(): void {
    if (this.form.invalid) {
      return;
    }

    const val = this.form.value;
    const updateDto: UpdateDiscordSettingsDto = {
      webhookUrl: val.webhookUrl || '',
      isEnabled: !!val.isEnabled,
      notifyOnNewTicket: !!val.notifyOnNewTicket,
      notifyOnCriticalOnly: !!val.notifyOnCriticalOnly,
      notifyOnAssigned: !!val.notifyOnAssigned,
      notifyOnSlaBreach: !!val.notifyOnSlaBreach,
      notifyOnResolved: !!val.notifyOnResolved,
      botName: val.botName || this.defaultBotName,
      avatarUrl: val.avatarUrl || this.defaultAvatarUrl,
    };

    this.isSaving = true;
    this.cdr.detectChanges();
    this.discordService.update(updateDto).subscribe({
      next: () => {
        this.toaster.success('Lưu cấu hình Discord thành công!', 'Thành công');
        this.isSaving = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toaster.error('Lỗi khi lưu cấu hình Discord: ' + (err.message || 'Lỗi server'));
        this.isSaving = false;
        this.cdr.detectChanges();
      },
    });
  }

  testWebhook(): void {
    const webhookUrl = this.form.get('webhookUrl')?.value;
    if (!webhookUrl || !webhookUrl.trim()) {
      this.toaster.warn('Vui lòng nhập Webhook URL để kiểm tra thử nghiệm.', 'Chưa có Webhook URL');
      return;
    }

    this.isTesting = true;
    this.cdr.detectChanges();
    this.discordService.sendTestNotification({ webhookUrl: webhookUrl.trim() }).subscribe({
      next: (res) => {
        this.isTesting = false;
        if (res.success) {
          this.toaster.success(res.message, 'Kết nối Discord thành công 🎉');
        } else {
          this.toaster.error(res.message, 'Kiểm tra thất bại');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isTesting = false;
        this.toaster.error('Không thể gửi tin nhắn thử: ' + (err.message || 'Lỗi mạng'));
        this.cdr.detectChanges();
      },
    });
  }
}
