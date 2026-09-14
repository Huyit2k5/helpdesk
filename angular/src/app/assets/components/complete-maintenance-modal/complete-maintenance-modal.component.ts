import { Component, EventEmitter, Input, Output, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToasterService } from '@abp/ng.theme.shared';
import { AssetMaintenanceService } from '../../../proxy/assets/asset-maintenance.service';
import { AssetDetailDto, AssetMaintenanceDto, CompleteAssetMaintenanceDto } from '../../../proxy/assets/models';

@Component({
  selector: 'app-complete-maintenance-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './complete-maintenance-modal.component.html',
  styleUrls: ['./complete-maintenance-modal.component.scss']
})
export class CompleteMaintenanceModalComponent implements OnChanges {
  private fb = inject(FormBuilder);
  private maintenanceService = inject(AssetMaintenanceService);
  private toaster = inject(ToasterService);

  @Input() asset: AssetDetailDto | null = null;
  @Input() maintenance: AssetMaintenanceDto | null = null;
  @Input() isOpen = false;
  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() maintenanceCompleted = new EventEmitter<void>();

  saving = false;

  completeForm: FormGroup = this.initForm();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['maintenance'] && this.maintenance) {
      this.populateForm();
    }
  }

  private initForm(): FormGroup {
    const today = new Date().toISOString().substring(0, 10);
    // Default next maintenance: 6 months later
    const nextDate = new Date();
    nextDate.setMonth(nextDate.getMonth() + 6);
    const nextDateStr = nextDate.toISOString().substring(0, 10);

    return this.fb.group({
      actualCost: [0, [Validators.required, Validators.min(0)]],
      actualCompletionDate: [today, Validators.required],
      replacedParts: ['', Validators.maxLength(1000)],
      partsWarrantyExpiryDate: [''],
      notes: ['', Validators.maxLength(1000)],
      returnToStock: [true],
      nextMaintenanceDate: [nextDateStr],
    });
  }

  private populateForm(): void {
    if (!this.maintenance) return;
    const initialCost = this.maintenance.actualCost ?? this.maintenance.estimatedCost ?? 0;
    this.completeForm.patchValue({
      actualCost: initialCost,
      actualCompletionDate: new Date().toISOString().substring(0, 10),
      replacedParts: this.maintenance.replacedParts || '',
      notes: this.maintenance.notes || '',
    });
  }

  close(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
  }

  save(): void {
    if (this.completeForm.invalid || !this.maintenance) {
      this.completeForm.markAllAsTouched();
      return;
    }

    const val = this.completeForm.value;
    const dto: CompleteAssetMaintenanceDto = {
      actualCost: Number(val.actualCost),
      actualCompletionDate: val.actualCompletionDate ? new Date(val.actualCompletionDate).toISOString() : new Date().toISOString(),
      replacedParts: val.replacedParts || null,
      partsWarrantyExpiryDate: val.partsWarrantyExpiryDate ? new Date(val.partsWarrantyExpiryDate).toISOString() : null,
      notes: val.notes || null,
      returnToStock: val.returnToStock ?? true,
      nextMaintenanceDate: val.nextMaintenanceDate ? new Date(val.nextMaintenanceDate).toISOString() : null,
    };

    this.saving = true;
    this.maintenanceService.complete(this.maintenance.id!, dto).subscribe({
      next: () => {
        this.saving = false;
        this.toaster.success('Đã nghiệm thu và hoàn tất sửa chữa / bảo trì thiết bị thành công!');
        this.close();
        this.maintenanceCompleted.emit();
      },
      error: (err) => {
        this.saving = false;
        const msg = err?.error?.error?.message || 'Không thể hoàn tất nghiệm thu. Vui lòng kiểm tra lại!';
        this.toaster.error(msg);
      }
    });
  }
}
