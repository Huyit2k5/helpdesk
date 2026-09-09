import { Component, EventEmitter, Input, Output, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { catchError, forkJoin, of } from 'rxjs';
import { TicketService } from '../../proxy/tickets/ticket.service';
import { CreateTicketDto } from '../../proxy/tickets/dtos/models';
import { CategoryService } from '../../proxy/categories/category.service';
import { CategoryLookupDto } from '../../proxy/categories/models';
import { PriorityService } from '../../proxy/priorities/priority.service';
import { PriorityDto } from '../../proxy/priorities/models';
import { TicketStatusService } from '../../proxy/ticket-statuses/ticket-status.service';
import { TicketStatusDto } from '../../proxy/ticket-statuses/models';
import { TicketSourceService } from '../../proxy/ticket-sources/ticket-source.service';
import { TicketSourceDto } from '../../proxy/ticket-sources/models';
import { DepartmentService } from '../../proxy/departments/department.service';
import { DepartmentDto } from '../../proxy/departments/models';
import { IdentityUserService } from '@abp/ng.identity/proxy';
import { IdentityUserDto } from '@abp/ng.identity/proxy';
import { ToasterService } from '@abp/ng.theme.shared';

@Component({
  selector: 'app-ticket-create-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './ticket-create-modal.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketCreateModalComponent implements OnInit {
  private fb = inject(FormBuilder);
  private ticketSvc = inject(TicketService);
  private categorySvc = inject(CategoryService);
  private prioritySvc = inject(PriorityService);
  private statusSvc = inject(TicketStatusService);
  private sourceSvc = inject(TicketSourceService);
  private departmentSvc = inject(DepartmentService);
  private userSvc = inject(IdentityUserService);
  private toaster = inject(ToasterService);
  private cdr = inject(ChangeDetectorRef);

  private _isOpen = false;
  @Input()
  get isOpen(): boolean {
    return this._isOpen;
  }
  set isOpen(val: boolean) {
    if (val && !this._isOpen) {
      this.resetAndInitForm();
    }
    this._isOpen = val;
  }

  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() saved = new EventEmitter<void>();

  isSaving = false;
  selectedFiles: File[] = [];
  categories: CategoryLookupDto[] = [];
  priorities: PriorityDto[] = [];
  statuses: TicketStatusDto[] = [];
  sources: TicketSourceDto[] = [];
  departments: DepartmentDto[] = [];
  users: IdentityUserDto[] = [];

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(256)]],
    description: ['', Validators.maxLength(4000)],
    categoryId: ['', Validators.required],
    priorityId: ['', Validators.required],
    statusId: ['', Validators.required],
    sourceId: ['', Validators.required],
    departmentId: [null],
    assigneeId: [null],
    requesterName: ['', [Validators.required, Validators.maxLength(128)]],
    requesterEmail: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    requesterPhone: ['', Validators.maxLength(32)],
    dueDate: [null],
    tags: ['', Validators.maxLength(256)],
  });

  ngOnInit(): void {
    this.loadLookups();
  }

  loadLookups(): void {
    this.categorySvc.getLookup().pipe(
      catchError(() => of({ items: [] }))
    ).subscribe(res => {
      this.categories = res.items ?? [];
      if (!this.form.get('categoryId')?.value && this.categories.length > 0) {
        this.form.patchValue({ categoryId: this.categories[0].id });
      }
      this.cdr.markForCheck();
    });

    this.prioritySvc.getList({ maxResultCount: 50, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.priorities = res.items ?? [];
      if (!this.form.get('priorityId')?.value && this.priorities.length > 0) {
        this.form.patchValue({ priorityId: this.priorities[0].id });
      }
      this.cdr.markForCheck();
    });

    this.statusSvc.getList({ maxResultCount: 50, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.statuses = res.items ?? [];
      const defaultStatus = this.statuses.find(s => s.isDefault) ?? this.statuses[0];
      if (!this.form.get('statusId')?.value && defaultStatus) {
        this.form.patchValue({ statusId: defaultStatus.id });
      }
      this.cdr.markForCheck();
    });

    this.sourceSvc.getList({ maxResultCount: 50, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.sources = res.items ?? [];
      if (!this.form.get('sourceId')?.value && this.sources.length > 0) {
        this.form.patchValue({ sourceId: this.sources[0].id });
      }
      this.cdr.markForCheck();
    });

    this.departmentSvc.getList({ maxResultCount: 50, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.departments = res.items ?? [];
      this.cdr.markForCheck();
    });

    this.userSvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.users = res.items ?? [];
      this.cdr.markForCheck();
    });
  }

  resetAndInitForm(): void {
    this.form.reset();
    this.selectedFiles = [];
    const defaultStatus = this.statuses.find(s => s.isDefault) ?? this.statuses[0];
    this.form.patchValue({
      priorityId: this.priorities[0]?.id || '',
      statusId: defaultStatus?.id || '',
      sourceId: this.sources[0]?.id || '',
      categoryId: this.categories[0]?.id || '',
    });
    this.cdr.markForCheck();
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      const files = Array.from(input.files);
      for (const file of files) {
        if (file.size > 10 * 1024 * 1024) {
          this.toaster.warn(`Tệp ${file.name} vượt quá giới hạn 10 MB.`, 'Cảnh báo');
          continue;
        }
        this.selectedFiles.push(file);
      }
      input.value = '';
      this.cdr.markForCheck();
    }
  }

  removeSelectedFile(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.cdr.markForCheck();
  }

  formatFileSize(bytes: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  open(): void {
    this.resetAndInitForm();
    this.isOpen = true;
    this.isOpenChange.emit(true);
    this.cdr.markForCheck();
  }

  close(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
  }

  save(): void {
    if (this.form.invalid || this.isSaving) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving = true;
    const val = this.form.value;
    const input: CreateTicketDto = {
      title: val.title,
      description: val.description || undefined,
      categoryId: val.categoryId,
      priorityId: val.priorityId,
      statusId: val.statusId,
      sourceId: val.sourceId,
      departmentId: val.departmentId || null,
      assigneeId: val.assigneeId || null,
      requesterName: val.requesterName,
      requesterEmail: val.requesterEmail,
      requesterPhone: val.requesterPhone || null,
      dueDate: val.dueDate ? new Date(val.dueDate).toISOString() : null,
      tags: val.tags || null,
    };

    this.ticketSvc.create(input).subscribe({
      next: (ticket) => {
        if (ticket.id && this.selectedFiles.length > 0) {
          const uploads = this.selectedFiles.map(f => this.ticketSvc.uploadAttachment(ticket.id!, f));
          forkJoin(uploads).pipe(
            catchError(() => of([]))
          ).subscribe(() => {
            this.isSaving = false;
            this.toaster.success(`Tạo yêu cầu #${ticket.ticketNumber} cùng ${this.selectedFiles.length} tệp đính kèm thành công!`, 'Thành công');
            this.close();
            this.saved.emit();
          });
        } else {
          this.isSaving = false;
          this.toaster.success(`Tạo yêu cầu #${ticket.ticketNumber} thành công!`, 'Thành công');
          this.close();
          this.saved.emit();
        }
      },
      error: () => {
        this.isSaving = false;
        this.cdr.markForCheck();
      }
    });
  }
}
