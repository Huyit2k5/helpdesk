import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { CannedResponseDto, CannedResponseGetListInput, CreateUpdateCannedResponseDto, CategoryLookupDto } from '../../proxy/helpdesk/models';
import { CannedResponseService } from '../../proxy/helpdesk/canned-response.service';
import { CategoryService } from '../../proxy/helpdesk/category.service';
import { DepartmentService } from '../../proxy/helpdesk/department.service';
import { DepartmentDto } from '../../proxy/helpdesk/models';

@Component({
  selector: 'app-canned-responses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './canned-responses.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CannedResponsesComponent implements OnInit {
  private svc = inject(CannedResponseService);
  private categorySvc = inject(CategoryService);
  private departmentSvc = inject(DepartmentService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<CannedResponseGetListInput>);

  items: CannedResponseDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';
  categories: CategoryLookupDto[] = [];
  departments: { id: string; name: string }[] = [];

  form: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    content: ['', [Validators.required, Validators.maxLength(5000)]],
    shortcut: ['', Validators.maxLength(50)],
    categoryId: [null],
    departmentId: [null],
    isActive: [true],
  });

  ngOnInit(): void {
    this.loadLookups();
    const streamCreator = (query: CannedResponseGetListInput) =>
      this.svc.getList({ ...query, filter: this.filterText });
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.items = res.items ?? [];
      this.totalCount = res.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  loadLookups(): void {
    this.categorySvc.getLookup().subscribe(d => { this.categories = d; this.cdr.markForCheck(); });
    this.departmentSvc.getList({ maxResultCount: 100, skipCount: 0 }).subscribe(d => {
      this.departments = (d.items ?? []).map(x => ({ id: x.id, name: x.name }));
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

  openEditModal(item: CannedResponseDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    this.form.patchValue({ ...item, categoryId: item.categoryId || null, departmentId: item.departmentId || null });
    this.isModalOpen = true;
  }

  closeModal(): void { this.isModalOpen = false; }

  save(): void {
    if (this.form.invalid) return;
    const input: CreateUpdateCannedResponseDto = this.form.value;
    const req = this.isEditing ? this.svc.update(this.selectedId!, input) : this.svc.create(input);
    req.subscribe(() => { this.isModalOpen = false; this.list.get(); });
  }

  delete(item: CannedResponseDto): void {
    this.confirmation.warn('::AreYouSureToDelete', '::AreYouSure', { messageLocalizationParams: [item.title] })
      .subscribe((s: Confirmation.Status) => {
        if (s === Confirmation.Status.confirm) this.svc.delete(item.id).subscribe(() => this.list.get());
      });
  }
}
