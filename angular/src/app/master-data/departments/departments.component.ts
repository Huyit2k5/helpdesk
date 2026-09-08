import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { DepartmentDto, DepartmentGetListInput, CreateUpdateDepartmentDto } from '../../proxy/departments/models';
import { DepartmentService } from '../../proxy/departments/department.service';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './departments.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DepartmentsComponent implements OnInit {
  private svc = inject(DepartmentService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<DepartmentGetListInput>);

  items: DepartmentDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';

  form: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(500)],
    managerId: [null],
    isActive: [true],
  });

  ngOnInit(): void {
    const streamCreator = (query: DepartmentGetListInput) =>
      this.svc.getList({ ...query, filter: this.filterText });
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.items = res.items ?? [];
      this.totalCount = res.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  search(): void { this.list.get(); }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedId = undefined;
    this.form.reset({ isActive: true });
    this.isModalOpen = true;
  }

  openEditModal(item: DepartmentDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    this.form.patchValue({
      code: item.code,
      name: item.name,
      description: item.description,
      managerId: item.managerId,
      isActive: item.isActive,
    });
    this.isModalOpen = true;
  }

  closeModal(): void { this.isModalOpen = false; }

  save(): void {
    if (this.form.invalid) return;
    const input: CreateUpdateDepartmentDto = this.form.value;
    const req = this.isEditing ? this.svc.update(this.selectedId!, input) : this.svc.create(input);
    req.subscribe(() => { this.isModalOpen = false; this.list.get(); });
  }

  delete(item: DepartmentDto): void {
    this.confirmation.warn('::AreYouSureToDelete', '::AreYouSure', { messageLocalizationParams: [item.name ?? ''] })
      .subscribe((s: Confirmation.Status) => {
        if (s === Confirmation.Status.confirm && item.id) this.svc.delete(item.id).subscribe(() => this.list.get());
      });
  }
}
