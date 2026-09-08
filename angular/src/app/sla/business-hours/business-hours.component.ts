import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { ConfirmationService, Confirmation, ToasterService } from '@abp/ng.theme.shared';
import { BusinessHourService } from '../../proxy/sla/business-hour.service';
import { BusinessHourDto, HolidayDto, CreateUpdateHolidayDto, UpdateBusinessHourDto } from '../../proxy/sla/dtos/models';

@Component({
  selector: 'app-business-hours',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './business-hours.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BusinessHoursComponent implements OnInit {
  private businessHourService = inject(BusinessHourService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private toaster = inject(ToasterService);
  private cdr = inject(ChangeDetectorRef);

  activeTab: 'hours' | 'holidays' = 'hours';

  // Business Hours
  businessHours: BusinessHourDto[] = [];
  hoursForm: FormGroup = this.fb.group({
    items: this.fb.array([]),
  });

  get hoursArray(): FormArray {
    return this.hoursForm.get('items') as FormArray;
  }

  // Holidays
  holidays: HolidayDto[] = [];
  isHolidayModalOpen = false;
  isEditingHoliday = false;
  selectedHolidayId?: string;

  holidayForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    date: ['', Validators.required],
    isRecurring: [false],
  });

  ngOnInit(): void {
    this.loadBusinessHours();
    this.loadHolidays();
  }

  loadBusinessHours(): void {
    this.businessHourService.getBusinessHours().subscribe(hours => {
      this.businessHours = hours;
      this.hoursArray.clear();
      for (const h of hours) {
        this.hoursArray.push(
          this.fb.group({
            id: [h.id],
            dayOfWeek: [h.dayOfWeek],
            dayOfWeekName: [this.getDayOfWeekVietnamese(h.dayOfWeek)],
            startTime: [h.startTime || '08:30', Validators.required],
            endTime: [h.endTime || '17:30', Validators.required],
            isWorkingDay: [h.isWorkingDay ?? true],
          })
        );
      }
      this.cdr.markForCheck();
    });
  }

  saveBusinessHours(): void {
    if (this.hoursForm.invalid) {
      return;
    }

    const payload: UpdateBusinessHourDto[] = this.hoursArray.value.map((v: any) => ({
      id: v.id,
      startTime: v.startTime,
      endTime: v.endTime,
      isWorkingDay: v.isWorkingDay,
    }));

    this.businessHourService.updateBusinessHours(payload).subscribe({
      next: () => {
        this.toaster.success('Cập nhật khung giờ làm việc thành công!', 'Thông báo');
        this.loadBusinessHours();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  loadHolidays(): void {
    this.businessHourService.getHolidays().subscribe(data => {
      this.holidays = data;
      this.cdr.markForCheck();
    });
  }

  openCreateHolidayModal(): void {
    this.isEditingHoliday = false;
    this.selectedHolidayId = undefined;
    this.holidayForm.reset({
      name: '',
      date: new Date().toISOString().substring(0, 10),
      isRecurring: true,
    });
    this.isHolidayModalOpen = true;
    this.cdr.markForCheck();
  }

  openEditHolidayModal(item: HolidayDto): void {
    this.isEditingHoliday = true;
    this.selectedHolidayId = item.id;
    this.holidayForm.patchValue({
      name: item.name,
      date: item.date ? item.date.substring(0, 10) : '',
      isRecurring: item.isRecurring ?? false,
    });
    this.isHolidayModalOpen = true;
    this.cdr.markForCheck();
  }

  closeHolidayModal(): void {
    this.isHolidayModalOpen = false;
    this.cdr.markForCheck();
  }

  saveHoliday(): void {
    if (this.holidayForm.invalid) {
      this.holidayForm.markAllAsTouched();
      return;
    }

    const payload: CreateUpdateHolidayDto = this.holidayForm.value;

    const request$ = this.isEditingHoliday && this.selectedHolidayId
      ? this.businessHourService.updateHoliday(this.selectedHolidayId, payload)
      : this.businessHourService.createHoliday(payload);

    request$.subscribe({
      next: () => {
        this.toaster.success(
          this.isEditingHoliday ? 'Cập nhật ngày nghỉ thành công!' : 'Thêm ngày nghỉ mới thành công!',
          'Thông báo'
        );
        this.closeHolidayModal();
        this.loadHolidays();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  deleteHoliday(item: HolidayDto): void {
    this.confirmation
      .warn(`Bạn có chắc muốn xóa ngày nghỉ "${item.name}"?`, 'Xác nhận xóa')
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm && item.id) {
          this.businessHourService.deleteHoliday(item.id).subscribe(() => {
            this.toaster.success('Đã xóa ngày nghỉ thành công.', 'Thông báo');
            this.loadHolidays();
          });
        }
      });
  }

  getDayOfWeekVietnamese(day?: any): string {
    const d = Number(day);
    switch (d) {
      case 0: return 'Chủ Nhật';
      case 1: return 'Thứ Hai';
      case 2: return 'Thứ Ba';
      case 3: return 'Thứ Tư';
      case 4: return 'Thứ Năm';
      case 5: return 'Thứ Sáu';
      case 6: return 'Thứ Bảy';
      default: return 'Ngày ' + day;
    }
  }
}
