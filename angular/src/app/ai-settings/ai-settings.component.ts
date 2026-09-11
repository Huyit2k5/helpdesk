import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { AiAssistantService } from '../proxy/ai/ai-assistant.service';
import { AiProviderType, AiSettingsDto, UpdateAiSettingsDto, TestAiConnectionResultDto } from '../proxy/ai/models';

@Component({
  selector: 'app-ai-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './ai-settings.component.html',
  styleUrls: ['./ai-settings.component.scss'],
})
export class AiSettingsComponent implements OnInit {
  private aiService = inject(AiAssistantService);
  private toaster = inject(ToasterService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  AiProviderType = AiProviderType;

  form!: FormGroup;
  isLoading = true;
  isSaving = false;
  isTesting = false;
  showApiKey = false;

  testResult: TestAiConnectionResultDto | null = null;

  providers = [
    {
      id: AiProviderType.BuiltInOffline,
      name: 'Built-in Smart NLP',
      badge: 'Offline • Sẵn Sàng 100%',
      desc: 'Công cụ xử lý ngôn ngữ tự nhiên tích hợp sẵn. Hoạt động offline, độ trễ cực thấp, không tốn chi phí và không phụ thuộc internet.',
      icon: 'fas fa-microchip',
      badgeClass: 'badge-soft-success'
    },
    {
      id: AiProviderType.GoogleGemini,
      name: 'Google Gemini',
      badge: 'Khuyên Dùng • Cực Nhanh',
      desc: 'Mô hình Gemini 1.5 Flash tiên tiến từ Google. Tốc độ vượt trội, xử lý tiếng Việt mượt mà, hỗ trợ context lớn.',
      icon: 'fab fa-google',
      badgeClass: 'badge-soft-primary'
    },
    {
      id: AiProviderType.OpenAI,
      name: 'OpenAI (ChatGPT)',
      badge: 'Tiêu Chuẩn Công Nghiệp',
      desc: 'Mô hình GPT-4o mini / GPT-4o thông minh, khả năng suy luận logic và soạn thảo giải pháp kỹ thuật tinh tế.',
      icon: 'fas fa-brain',
      badgeClass: 'badge-soft-info'
    },
    {
      id: AiProviderType.LocalOllama,
      name: 'Local Ollama (Tự Máy Chủ)',
      badge: 'Riêng Tư Nội Bộ 100%',
      desc: 'Chạy LLM cục bộ trên máy chủ doanh nghiệp (Llama 3, Qwen 2.5, Mistral...). Tuyệt đối an toàn dữ liệu.',
      icon: 'fas fa-server',
      badgeClass: 'badge-soft-warning'
    }
  ];

  ngOnInit(): void {
    this.buildForm();
    this.loadSettings();
  }

  buildForm(): void {
    this.form = this.fb.group({
      isEnabled: [true],
      provider: [AiProviderType.BuiltInOffline, Validators.required],
      apiKey: [''],
      modelName: ['gemini-1.5-flash'],
      baseUrl: [''],
      temperature: [0.3, [Validators.min(0), Validators.max(1)]],
      enableAutoSentiment: [true],
      customSystemPrompt: ['']
    });

    this.form.get('provider')?.valueChanges.subscribe((p: AiProviderType) => {
      this.onProviderChanged(p);
    });
  }

  onProviderChanged(provider: AiProviderType): void {
    const modelCtrl = this.form.get('modelName');
    const baseCtrl = this.form.get('baseUrl');

    if (provider === AiProviderType.GoogleGemini) {
      if (!modelCtrl?.value || modelCtrl?.value === 'gpt-4o-mini' || modelCtrl?.value === 'llama3') {
        modelCtrl?.setValue('gemini-1.5-flash');
      }
    } else if (provider === AiProviderType.OpenAI) {
      if (!modelCtrl?.value || modelCtrl?.value === 'gemini-1.5-flash' || modelCtrl?.value === 'llama3') {
        modelCtrl?.setValue('gpt-4o-mini');
      }
    } else if (provider === AiProviderType.LocalOllama) {
      if (!modelCtrl?.value || modelCtrl?.value === 'gemini-1.5-flash' || modelCtrl?.value === 'gpt-4o-mini') {
        modelCtrl?.setValue('llama3:latest');
      }
      if (!baseCtrl?.value) {
        baseCtrl?.setValue('http://localhost:11434');
      }
    }
  }

  selectProvider(provider: AiProviderType): void {
    this.form.get('provider')?.setValue(provider);
  }

  loadSettings(): void {
    this.isLoading = true;
    this.cdr.detectChanges();

    this.aiService.getSettings().subscribe({
      next: (settings: AiSettingsDto) => {
        this.form.patchValue({
          isEnabled: settings.isEnabled,
          provider: settings.provider,
          apiKey: settings.apiKey || '',
          modelName: settings.modelName || 'gemini-1.5-flash',
          baseUrl: settings.baseUrl || '',
          temperature: settings.temperature ?? 0.3,
          enableAutoSentiment: settings.enableAutoSentiment,
          customSystemPrompt: settings.customSystemPrompt || ''
        });
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.toaster.error('Không thể tải cấu hình AI Copilot.');
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleApiKeyVisibility(): void {
    this.showApiKey = !this.showApiKey;
  }

  saveSettings(): void {
    if (this.form.invalid) {
      this.toaster.warn('Vui lòng kiểm tra lại các trường dữ liệu.');
      return;
    }

    this.isSaving = true;
    const val = this.form.value;
    const input: UpdateAiSettingsDto = {
      isEnabled: !!val.isEnabled,
      provider: Number(val.provider),
      apiKey: val.apiKey ? val.apiKey.trim() : '',
      modelName: val.modelName ? val.modelName.trim() : '',
      baseUrl: val.baseUrl ? val.baseUrl.trim() : '',
      temperature: Number(val.temperature) || 0.3,
      enableAutoSentiment: !!val.enableAutoSentiment,
      customSystemPrompt: val.customSystemPrompt ? val.customSystemPrompt.trim() : ''
    };

    this.aiService.updateSettings(input).subscribe({
      next: () => {
        this.isSaving = false;
        this.toaster.success('Đã lưu cấu hình AI Copilot thành công!');
        this.loadSettings();
      },
      error: (err) => {
        this.isSaving = false;
        this.toaster.error(err?.error?.message || 'Có lỗi xảy ra khi lưu cấu hình.');
        this.cdr.detectChanges();
      }
    });
  }

  testConnection(): void {
    this.isTesting = true;
    this.testResult = null;
    this.cdr.detectChanges();

    const val = this.form.value;
    this.aiService.testConnection({
      provider: Number(val.provider),
      apiKey: val.apiKey ? val.apiKey.trim() : undefined,
      modelName: val.modelName ? val.modelName.trim() : undefined,
      baseUrl: val.baseUrl ? val.baseUrl.trim() : undefined
    }).subscribe({
      next: (res: TestAiConnectionResultDto) => {
        this.isTesting = false;
        this.testResult = res;
        if (res.success) {
          this.toaster.success(`Kết nối AI thành công! Phản hồi trong ${res.latencyMs}ms`);
        } else {
          this.toaster.error(res.message);
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isTesting = false;
        this.testResult = {
          success: false,
          message: err?.error?.message || 'Lỗi mạng hoặc không kết nối được tới máy chủ AI.',
          latencyMs: 0
        };
        this.toaster.error(this.testResult.message);
        this.cdr.detectChanges();
      }
    });
  }
}
