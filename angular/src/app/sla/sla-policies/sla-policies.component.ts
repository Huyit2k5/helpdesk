import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { ListService, PagedResultDto, LocalizationPipe } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { SlaPolicyService } from '../../proxy/sla/sla-policy.service';
import { SlaPolicyDto, CreateUpdateSlaPolicyDto, GetSlaPolicyListInput } from '../../proxy/sla/dtos/models';
import { PriorityService } from '../../proxy/priorities/priority.service';
import { PriorityDto } from '../../proxy/priorities/models';
import { CategoryService } from '../../proxy/categories/category.service';
import { CategoryDto } from '../../proxy/categories/models';

@Component({
  selector: 'app-sla-policies',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './sla-policies.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlaPoliciesComponent implements OnInit {
  private slaPolicyService = inject(SlaPolicyService);
  private priorityService = inject(PriorityService);
  private categoryService = inject(CategoryService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<GetSlaPolicyListInput>);

  items: SlaPolicyDto[] = [];
  totalCount = 0;
  priorities: PriorityDto[] = [];
  categories: CategoryDto[] = [];

  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    description: ['', Validators.maxLength(500)],
    isDefault: [false],
    isActive: [true],
    rules: this.fb.array([]),
  });

  get rulesArray(): FormArray {
    return this.form.get('rules') as FormArray;
  }

  ngOnInit(): void {
    this.loadLookups();

    const streamCreator = (query: GetSlaPolicyListInput) =>
      this.slaPolicyService.getList({ ...query, filter: this.filterText });

    this.list.hookToQuery(streamCreator).subscribe(response => {
      this.items = response.items ?? [];
      this.totalCount = response.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  loadLookups(): void {
    this.priorityService.getList({ maxResultCount: 100 }).subscribe(res => {
      this.priorities = res.items ?? [];
      this.cdr.markForCheck();
    });

    this.categoryService.getList({ maxResultCount: 100 }).subscribe(res => {
      this.categories = res.items ?? [];
      this.cdr.markForCheck();
    });
  }

  search(): void {
    this.list.get();
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedId = undefined;
    this.form.reset({
      name: '',
      description: '',
      isDefault: false,
      isActive: true,
    });
    this.rulesArray.clear();
    // Populate default rules for all priorities
    this.populateDefaultRules();
    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  populateDefaultRules(): void {
    for (const pri of this.priorities) {
      this.rulesArray.push(
        this.fb.group({
          priorityId: [pri.id, Validators.required],
          categoryId: [null],
          responseTimeMinutes: [(pri.slaResponseHours || 4) * 60, [Validators.required, Validators.min(1)]],
          resolutionTimeMinutes: [(pri.slaResolutionHours || 24) * 60, [Validators.required, Validators.min(1)]],
        })
      );
    }
  }

  addRule(): void {
    const firstPri = this.priorities.length > 0 ? this.priorities[0].id : null;
    this.rulesArray.push(
      this.fb.group({
        priorityId: [firstPri, Validators.required],
        categoryId: [null],
        responseTimeMinutes: [240, [Validators.required, Validators.min(1)]],
        resolutionTimeMinutes: [1440, [Validators.required, Validators.min(1)]],
      })
    );
  }

  removeRule(index: number): void {
    this.rulesArray.removeAt(index);
  }

  openEditModal(item: SlaPolicyDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    this.slaPolicyService.get(item.id!).subscribe(policy => {
      this.form.patchValue({
        name: policy.name,
        description: policy.description,
        isDefault: policy.isDefault,
        isActive: policy.isActive,
      });

      this.rulesArray.clear();
      if (policy.rules && policy.rules.length > 0) {
        for (const r of policy.rules) {
          this.rulesArray.push(
            this.fb.group({
              id: [r.id],
              priorityId: [r.priorityId, Validators.required],
              categoryId: [r.categoryId],
              responseTimeMinutes: [r.responseTimeMinutes, [Validators.required, Validators.min(1)]],
              resolutionTimeMinutes: [r.resolutionTimeMinutes, [Validators.required, Validators.min(1)]],
            })
          );
        }
      }

      this.isModalOpen = true;
      this.cdr.markForCheck();
    });
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: CreateUpdateSlaPolicyDto = this.form.value;

    const request$ = this.isEditing && this.selectedId
      ? this.slaPolicyService.update(this.selectedId, payload)
      : this.slaPolicyService.create(payload);

    request$.subscribe({
      next: () => {
        this.closeModal();
        this.list.get();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  deletePolicy(item: SlaPolicyDto): void {
    this.confirmation
      .warn(`Bạn có chắc chắn muốn xóa chính sách SLA "${item.name}"?`, 'Xác nhận xóa')
      .subscribe((status: Confirmation.Status) => {
        if (status === Confirmation.Status.confirm && item.id) {
          this.slaPolicyService.delete(item.id).subscribe(() => {
            this.list.get();
          });
        }
      });
  }

  getPriorityName(id: string): string {
    return this.priorities.find(p => p.id === id)?.name || '—';
  }

  getPriorityColor(id: string): string {
    return this.priorities.find(p => p.id === id)?.color || '#64748b';
  }

  getCategoryName(id: string | null): string {
    if (!id) return 'Tất cả danh mục';
    return this.categories.find(c => c.id === id)?.name || '—';
  }

  formatMinutes(mins?: number): string {
    if (!mins) return '—';
    if (mins < 60) return `${mins} phút`;
    const hours = Math.floor(mins / 60);
    const remainMins = mins % 60;
    return remainMins > 0 ? `${hours}h ${remainMins}m` : `${hours} giờ`;
  }
}
