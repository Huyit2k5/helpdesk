import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { AssetAuditService } from '../../../proxy/assets/asset-audit.service';
import {
  AssetAuditSessionDto,
  AssetAuditStatus,
  AuditItemResult,
  CreateAssetAuditSessionDto,
  GetAssetAuditListInput,
} from '../../../proxy/assets/models';
import { DepartmentService } from '../../../proxy/departments/department.service';
import { DepartmentDto } from '../../../proxy/departments/models';

@Component({
  selector: 'app-asset-audit-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './asset-audit-list.component.html',
  styleUrls: ['./asset-audit-list.component.scss'],
})
export class AssetAuditListComponent implements OnInit {
  private auditService = inject(AssetAuditService);
  private departmentService = inject(DepartmentService);
  private toaster = inject(ToasterService);
  private confirmation = inject(ConfirmationService);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  sessions: AssetAuditSessionDto[] = [];
  departments: DepartmentDto[] = [];
  loading = false;
  totalCount = 0;

  // Filters
  searchTerm = '';
  selectedStatus: AssetAuditStatus | null = null;
  selectedDepartment = '';

  // Enums
  AssetAuditStatus = AssetAuditStatus;
  AuditItemResult = AuditItemResult;

  // Create Modal
  isCreateModalOpen = false;
  createForm!: FormGroup;
  isSaving = false;

  // Quick stats
  totalSessions = 0;
  inProgressCount = 0;
  completedCount = 0;

  ngOnInit(): void {
    this.initForm();
    this.loadDepartments();
    this.loadSessions();
  }

  initForm(): void {
    this.createForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(256)]],
      scopeDepartment: [''],
      scopeLocation: [''],
      notes: [''],
    });
  }

  loadDepartments(): void {
    this.departmentService.getList({ maxResultCount: 100 }).subscribe({
      next: res => {
        this.departments = res.items || [];
        this.cdr.markForCheck();
      },
      error: () => {},
    });
  }

  loadSessions(): void {
    this.loading = true;
    const input: GetAssetAuditListInput = {
      filter: this.searchTerm || undefined,
      status: this.selectedStatus ?? undefined,
      department: this.selectedDepartment || undefined,
      maxResultCount: 100,
    };

    this.auditService.getList(input).subscribe({
      next: res => {
        this.sessions = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.calculateStats();
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: err => {
        this.loading = false;
        this.toaster.error('Không thể tải danh sách đợt kiểm kê.');
        this.cdr.markForCheck();
      },
    });
  }

  calculateStats(): void {
    this.totalSessions = this.sessions.length;
    this.inProgressCount = this.sessions.filter(s => s.status === AssetAuditStatus.InProgress).length;
    this.completedCount = this.sessions.filter(s => s.status === AssetAuditStatus.Completed).length;
  }

  onFilterChange(): void {
    this.loadSessions();
  }

  openCreateModal(): void {
    const today = new Date().toLocaleDateString('vi-VN');
    this.createForm.reset({
      title: `Kiểm kê tài sản định kỳ - ${today}`,
      scopeDepartment: '',
      scopeLocation: '',
      notes: '',
    });
    this.isCreateModalOpen = true;
  }

  closeCreateModal(): void {
    this.isCreateModalOpen = false;
    this.createForm.reset();
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const input: CreateAssetAuditSessionDto = {
      title: this.createForm.value.title,
      scopeDepartment: this.createForm.value.scopeDepartment || null,
      scopeLocation: this.createForm.value.scopeLocation || null,
      notes: this.createForm.value.notes || null,
    };

    this.auditService.create(input).subscribe({
      next: session => {
        this.isSaving = false;
        this.closeCreateModal();
        this.toaster.success(`Đã tạo thành công đợt kiểm kê ${session.auditCode} với ${session.totalExpectedCount} thiết bị trong phạm vi!`);
        this.router.navigate(['/assets/audits', session.id]);
      },
      error: err => {
        this.isSaving = false;
        this.toaster.error(err?.error?.message || 'Lỗi khi khởi tạo đợt kiểm kê.');
        this.cdr.markForCheck();
      },
    });
  }

  startAudit(session: AssetAuditSessionDto, event: Event): void {
    event.stopPropagation();
    if (!session.id) return;
    this.confirmation
      .warn(`Bắt đầu đợt kiểm kê [${session.auditCode}] ngay bây giờ?`, 'Xác nhận tiến hành')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.auditService.start(session.id!).subscribe({
            next: updated => {
              this.toaster.success(`Đợt kiểm kê ${session.auditCode} đã bắt đầu!`);
              this.loadSessions();
            },
            error: () => this.toaster.error('Không thể bắt đầu đợt kiểm kê.'),
          });
        }
      });
  }

  cancelAudit(session: AssetAuditSessionDto, event: Event): void {
    event.stopPropagation();
    if (!session.id) return;
    this.confirmation
      .warn(`Bạn có chắc chắn muốn hủy đợt kiểm kê [${session.auditCode}]?`, 'Xác nhận hủy')
      .subscribe(status => {
        if (status === Confirmation.Status.confirm) {
          this.auditService.cancel(session.id!, 'Người dùng hủy trên giao diện').subscribe({
            next: () => {
              this.toaster.info(`Đã hủy đợt kiểm kê ${session.auditCode}.`);
              this.loadSessions();
            },
            error: () => this.toaster.error('Không thể hủy đợt kiểm kê.'),
          });
        }
      });
  }

  getStatusBadgeClass(status: AssetAuditStatus): string {
    switch (status) {
      case AssetAuditStatus.Draft:
        return 'badge-subtle-secondary';
      case AssetAuditStatus.InProgress:
        return 'badge-subtle-warning';
      case AssetAuditStatus.Completed:
        return 'badge-subtle-success';
      case AssetAuditStatus.Cancelled:
        return 'badge-subtle-danger';
      default:
        return 'badge-subtle-secondary';
    }
  }
}
