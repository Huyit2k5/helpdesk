import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { AssetMaintenanceService } from '../../../proxy/assets/asset-maintenance.service';
import { AssetDetailDto, CreateAssetMaintenanceDto, MaintenanceType } from '../../../proxy/assets/models';

@Component({
  selector: 'app-asset-maintenance-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './asset-maintenance-modal.component.html',
  styleUrls: ['./asset-maintenance-modal.component.scss']
})
export class AssetMaintenanceModalComponent {
  private fb = inject(FormBuilder);
  private maintenanceService = inject(AssetMaintenanceService);
  private toaster = inject(ToasterService);

  @Input() asset: AssetDetailDto | null = null;
  @Input() isOpen = false;
  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() maintenanceCreated = new EventEmitter<void>();

  saving = false;

  MaintenanceType = MaintenanceType;
  maintenanceTypes = [
    { value: MaintenanceType.Repair, label: 'Sửa chữa / Khắc phục sự cố', icon: 'fas fa-tools text-danger' },
    { value: MaintenanceType.Preventive, label: 'Bảo trì dự phòng định kỳ', icon: 'fas fa-calendar-check text-success' },
    { value: MaintenanceType.Upgrade, label: 'Nâng cấp phần cứng / Cấu hình', icon: 'fas fa-arrow-circle-up text-primary' },
    { value: MaintenanceType.Inspection, label: 'Kiểm tra & Đánh giá chất lượng', icon: 'fas fa-search text-warning' },
  ];

  maintenanceForm: FormGroup = this.fb.group({
    maintenanceType: [MaintenanceType.Repair, Validators.required],
    title: ['', [Validators.required, Validators.maxLength(128)]],
    serviceProvider: ['', Validators.maxLength(128)],
    trackingNumber: ['', Validators.maxLength(64)],
    startDate: [new Date().toISOString().substring(0, 10), Validators.required],
    expectedCompletionDate: [''],
    estimatedCost: [0, [Validators.min(0)]],
    description: ['', Validators.maxLength(2000)],
    notes: ['', Validators.maxLength(1000)],
    setAssetUnderRepair: [true]
  });

  close(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
  }

  save(): void {
    if (this.maintenanceForm.invalid || !this.asset) {
      this.maintenanceForm.markAllAsTouched();
      return;
    }

    const val = this.maintenanceForm.value;
    const dto: CreateAssetMaintenanceDto = {
      assetId: this.asset.id!,
      maintenanceType: Number(val.maintenanceType),
      title: val.title,
      serviceProvider: val.serviceProvider || null,
      trackingNumber: val.trackingNumber || null,
      startDate: new Date(val.startDate).toISOString(),
      expectedCompletionDate: val.expectedCompletionDate ? new Date(val.expectedCompletionDate).toISOString() : null,
      estimatedCost: val.estimatedCost != null && val.estimatedCost !== '' ? Number(val.estimatedCost) : null,
      description: val.description || null,
      notes: val.notes || null,
      setAssetUnderRepair: val.setAssetUnderRepair ?? true,
    };

    this.saving = true;
    this.maintenanceService.create(dto).subscribe({
      next: () => {
        this.saving = false;
        this.toaster.success('Đã lập phiếu bảo trì / sửa chữa thiết bị thành công!');
        this.close();
        this.maintenanceCreated.emit();
      },
      error: (err) => {
        this.saving = false;
        const msg = err?.error?.error?.message || 'Không thể tạo phiếu sửa chữa. Vui lòng thử lại!';
        this.toaster.error(msg);
      }
    });
  }
}
