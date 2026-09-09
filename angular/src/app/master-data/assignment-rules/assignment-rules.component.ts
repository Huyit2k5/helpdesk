import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { AssignmentRuleService } from '../../proxy/assignment-rules/assignment-rule.service';
import { AgentLookupDto, AssignmentRuleDto, AssignmentRuleGetListInput, AssignmentStrategy, CreateUpdateAssignmentRuleDto } from '../../proxy/assignment-rules/models';
import { CategoryService } from '../../proxy/categories/category.service';
import { CategoryLookupDto } from '../../proxy/categories/models';
import { PriorityService } from '../../proxy/priorities/priority.service';
import { PriorityDto } from '../../proxy/priorities/models';
import { DepartmentService } from '../../proxy/departments/department.service';
import { DepartmentDto } from '../../proxy/departments/models';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-assignment-rules',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './assignment-rules.component.html',
  styleUrls: ['./assignment-rules.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignmentRulesComponent implements OnInit {
  private svc = inject(AssignmentRuleService);
  private categorySvc = inject(CategoryService);
  private prioritySvc = inject(PriorityService);
  private departmentSvc = inject(DepartmentService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<AssignmentRuleGetListInput>);

  items: AssignmentRuleDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';

  categories: CategoryLookupDto[] = [];
  priorities: PriorityDto[] = [];
  departments: DepartmentDto[] = [];
  agents: AgentLookupDto[] = [];

  AssignmentStrategy = AssignmentStrategy;

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    description: ['', [Validators.maxLength(512)]],
    order: [1, [Validators.required, Validators.min(0)]],
    isActive: [true],
    routingStrategy: [AssignmentStrategy.RoundRobin, [Validators.required]],
    departmentId: [null],
    categoryId: [null],
    priorityId: [null],
    directAssigneeId: [null],
    selectedAgentIds: [[] as string[]],
  });

  ngOnInit(): void {
    this.loadLookups();
    const streamCreator = (query: AssignmentRuleGetListInput) =>
      this.svc.getList({ ...query, filter: this.filterText });
    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.items = res.items ?? [];
      this.totalCount = res.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  loadLookups(): void {
    this.categorySvc.getLookup().pipe(
      catchError(() => of({ items: [] }))
    ).subscribe(d => {
      this.categories = d.items ?? [];
      this.cdr.markForCheck();
    });
    this.prioritySvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(d => {
      this.priorities = d.items ?? [];
      this.cdr.markForCheck();
    });
    this.departmentSvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(d => {
      this.departments = d.items ?? [];
      this.cdr.markForCheck();
    });
    this.svc.getAgentLookup().pipe(
      catchError(() => of([]))
    ).subscribe(d => {
      this.agents = d ?? [];
      this.cdr.markForCheck();
    });
  }

  onFilter(): void {
    this.list.get();
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedId = undefined;
    this.form.reset({
      name: '',
      description: '',
      order: (this.items.length || 0) + 1,
      isActive: true,
      routingStrategy: AssignmentStrategy.RoundRobin,
      departmentId: null,
      categoryId: null,
      priorityId: null,
      directAssigneeId: null,
      selectedAgentIds: this.agents.map(a => a.id),
    });
    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  openEditModal(item: AssignmentRuleDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    const selectedAgentIds = (item.agents || []).map(a => a.userId);
    this.form.reset({
      name: item.name,
      description: item.description ?? '',
      order: item.order,
      isActive: item.isActive,
      routingStrategy: item.routingStrategy,
      departmentId: item.departmentId ?? null,
      categoryId: item.categoryId ?? null,
      priorityId: item.priorityId ?? null,
      directAssigneeId: item.directAssigneeId ?? null,
      selectedAgentIds: selectedAgentIds,
    });
    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.cdr.markForCheck();
  }

  toggleAgentSelection(userId: string): void {
    const current: string[] = this.form.get('selectedAgentIds')?.value ?? [];
    const index = current.indexOf(userId);
    if (index >= 0) {
      current.splice(index, 1);
    } else {
      current.push(userId);
    }
    this.form.patchValue({ selectedAgentIds: [...current] });
    this.cdr.markForCheck();
  }

  isAgentSelected(userId: string): boolean {
    const current: string[] = this.form.get('selectedAgentIds')?.value ?? [];
    return current.includes(userId);
  }

  selectAllAgents(): void {
    this.form.patchValue({ selectedAgentIds: this.agents.map(a => a.id) });
    this.cdr.markForCheck();
  }

  deselectAllAgents(): void {
    this.form.patchValue({ selectedAgentIds: [] });
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) return;

    const val = this.form.value;
    const input: CreateUpdateAssignmentRuleDto = {
      name: val.name,
      description: val.description || null,
      order: val.order,
      isActive: val.isActive,
      routingStrategy: Number(val.routingStrategy),
      departmentId: val.departmentId || null,
      categoryId: val.categoryId || null,
      priorityId: val.priorityId || null,
      directAssigneeId: val.routingStrategy === AssignmentStrategy.DirectAssign ? (val.directAssigneeId || null) : null,
      agentUserIds: val.routingStrategy !== AssignmentStrategy.DirectAssign ? (val.selectedAgentIds || []) : [],
    };

    const action = this.isEditing && this.selectedId
      ? this.svc.update(this.selectedId, input)
      : this.svc.create(input);

    action.subscribe(() => {
      this.closeModal();
      this.list.get();
    });
  }

  toggleActive(item: AssignmentRuleDto): void {
    if (!item.id) return;
    this.svc.toggleActive(item.id).subscribe(() => {
      this.list.get();
    });
  }

  delete(item: AssignmentRuleDto): void {
    if (!item.id) return;
    const ruleId = item.id;
    this.confirmation.warn(
      `Bạn có chắc chắn muốn xóa quy tắc phân công "${item.name}"?`,
      'Xác nhận xóa'
    ).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.svc.delete(ruleId).subscribe(() => this.list.get());
      }
    });
  }

  getStrategyBadgeClass(strategy: AssignmentStrategy): string {
    switch (strategy) {
      case AssignmentStrategy.RoundRobin: return 'badge bg-primary';
      case AssignmentStrategy.LeastBusy: return 'badge bg-success';
      case AssignmentStrategy.DirectAssign: return 'badge bg-warning text-dark';
      default: return 'badge bg-secondary';
    }
  }

  getStrategyIcon(strategy: AssignmentStrategy): string {
    switch (strategy) {
      case AssignmentStrategy.RoundRobin: return 'fas fa-sync-alt';
      case AssignmentStrategy.LeastBusy: return 'fas fa-balance-scale';
      case AssignmentStrategy.DirectAssign: return 'fas fa-user-check';
      default: return 'fas fa-random';
    }
  }
}
