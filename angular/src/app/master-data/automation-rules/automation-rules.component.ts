import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { AutomationRuleService } from '../../proxy/automations/automation-rule.service';
import {
  AutomationActionType,
  AutomationRuleDto,
  AutomationTriggerType,
  ConditionField,
  ConditionOperator,
  CreateUpdateAutomationRuleDto,
  GetAutomationRuleListInput,
  RuleAction,
  RuleCondition
} from '../../proxy/automations/models';
import { CategoryService } from '../../proxy/categories/category.service';
import { CategoryLookupDto } from '../../proxy/categories/models';
import { PriorityService } from '../../proxy/priorities/priority.service';
import { PriorityDto } from '../../proxy/priorities/models';
import { DepartmentService } from '../../proxy/departments/department.service';
import { DepartmentDto } from '../../proxy/departments/models';
import { TicketStatusService } from '../../proxy/ticket-statuses/ticket-status.service';
import { TicketStatusDto } from '../../proxy/ticket-statuses/models';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-automation-rules',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './automation-rules.component.html',
  styleUrls: ['./automation-rules.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutomationRulesComponent implements OnInit {
  private svc = inject(AutomationRuleService);
  private categorySvc = inject(CategoryService);
  private prioritySvc = inject(PriorityService);
  private departmentSvc = inject(DepartmentService);
  private statusSvc = inject(TicketStatusService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<GetAutomationRuleListInput>);

  items: AutomationRuleDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';
  selectedTriggerFilter?: AutomationTriggerType;

  categories: CategoryLookupDto[] = [];
  priorities: PriorityDto[] = [];
  departments: DepartmentDto[] = [];
  statuses: TicketStatusDto[] = [];

  AutomationTriggerType = AutomationTriggerType;
  ConditionField = ConditionField;
  ConditionOperator = ConditionOperator;
  AutomationActionType = AutomationActionType;

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    description: ['', [Validators.maxLength(512)]],
    triggerType: [AutomationTriggerType.OnTicketCreated, [Validators.required]],
    executionOrder: [1, [Validators.required, Validators.min(0)]],
    isActive: [true],
    stopProcessing: [false],
    conditions: this.fb.array([]),
    actions: this.fb.array([]),
  });

  get conditionsArray(): FormArray {
    return this.form.get('conditions') as FormArray;
  }

  get actionsArray(): FormArray {
    return this.form.get('actions') as FormArray;
  }

  ngOnInit(): void {
    this.loadLookups();
    const streamCreator = (query: GetAutomationRuleListInput) =>
      this.svc.getList({
        ...query,
        filter: this.filterText,
        triggerType: this.selectedTriggerFilter,
      });

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.items = res.items ?? [];
      this.totalCount = res.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  loadLookups(): void {
    this.categorySvc.getLookup().pipe(catchError(() => of({ items: [] }))).subscribe(d => {
      this.categories = d.items ?? [];
      this.cdr.markForCheck();
    });
    this.prioritySvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))).subscribe(d => {
      this.priorities = d.items ?? [];
      this.cdr.markForCheck();
    });
    this.departmentSvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))).subscribe(d => {
      this.departments = d.items ?? [];
      this.cdr.markForCheck();
    });
    this.statusSvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))).subscribe(d => {
      this.statuses = d.items ?? [];
      this.cdr.markForCheck();
    });
  }

  onFilter(): void {
    this.list.get();
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedId = undefined;
    this.conditionsArray.clear();
    this.actionsArray.clear();

    this.form.reset({
      name: '',
      description: '',
      triggerType: AutomationTriggerType.OnTicketCreated,
      executionOrder: this.items.length + 1,
      isActive: true,
      stopProcessing: false,
    });

    // Thêm sẵn 1 condition và 1 action mặc định
    this.addCondition();
    this.addAction();

    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  openEditModal(rule: AutomationRuleDto): void {
    this.isEditing = true;
    this.selectedId = rule.id;
    this.conditionsArray.clear();
    this.actionsArray.clear();

    this.form.patchValue({
      name: rule.name,
      description: rule.description,
      triggerType: rule.triggerType,
      executionOrder: rule.executionOrder,
      isActive: rule.isActive,
      stopProcessing: rule.stopProcessing,
    });

    if (rule.conditions && rule.conditions.length > 0) {
      rule.conditions.forEach(c => this.addCondition(c));
    } else {
      this.addCondition();
    }

    if (rule.actions && rule.actions.length > 0) {
      rule.actions.forEach(a => this.addAction(a));
    } else {
      this.addAction();
    }

    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  closeModal(): void {
    this.isModalOpen = false;
    this.cdr.markForCheck();
  }

  addCondition(condition?: RuleCondition): void {
    const group = this.fb.group({
      field: [condition?.field ?? ConditionField.Title, [Validators.required]],
      operator: [condition?.operator ?? ConditionOperator.Contains, [Validators.required]],
      value: [condition?.value ?? '', [Validators.required]],
    });
    this.conditionsArray.push(group);
    this.cdr.markForCheck();
  }

  removeCondition(index: number): void {
    this.conditionsArray.removeAt(index);
    this.cdr.markForCheck();
  }

  addAction(action?: RuleAction): void {
    const group = this.fb.group({
      actionType: [action?.actionType ?? AutomationActionType.ChangePriority, [Validators.required]],
      targetValue: [action?.targetValue ?? '', [Validators.required]],
      additionalValue: [action?.additionalValue ?? 'false'],
    });
    this.actionsArray.push(group);
    this.cdr.markForCheck();
  }

  removeAction(index: number): void {
    this.actionsArray.removeAt(index);
    this.cdr.markForCheck();
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.value;
    const dto: CreateUpdateAutomationRuleDto = {
      name: val.name,
      description: val.description,
      triggerType: Number(val.triggerType),
      executionOrder: Number(val.executionOrder),
      isActive: Boolean(val.isActive),
      stopProcessing: Boolean(val.stopProcessing),
      conditions: (val.conditions ?? []).map((c: any) => ({
        field: Number(c.field),
        operator: Number(c.operator),
        value: c.value ? String(c.value) : undefined,
      })),
      actions: (val.actions ?? []).map((a: any) => ({
        actionType: Number(a.actionType),
        targetValue: a.targetValue ? String(a.targetValue) : undefined,
        additionalValue: a.additionalValue ? String(a.additionalValue) : undefined,
      })),
    };

    const req$ = this.isEditing && this.selectedId
      ? this.svc.update(this.selectedId, dto)
      : this.svc.create(dto);

    req$.subscribe({
      next: () => {
        this.closeModal();
        this.list.get();
      },
      error: err => {
        console.error('Lỗi khi lưu quy tắc tự động hóa:', err);
      },
    });
  }

  toggleActive(rule: AutomationRuleDto, event: Event): void {
    event.stopPropagation();
    if (!rule.id) return;
    this.svc.toggleActive(rule.id).subscribe(() => {
      rule.isActive = !rule.isActive;
      this.cdr.markForCheck();
    });
  }

  delete(rule: AutomationRuleDto): void {
    if (!rule.id) return;
    this.confirmation.warn(
      `Bạn có chắc chắn muốn xóa quy tắc "${rule.name}" không?`,
      'Xác nhận xóa'
    ).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.svc.delete(rule.id!).subscribe(() => {
          this.list.get();
        });
      }
    });
  }

  getTriggerBadgeClass(trigger: AutomationTriggerType): string {
    switch (trigger) {
      case AutomationTriggerType.OnTicketCreated:
        return 'badge bg-primary-subtle text-primary border border-primary';
      case AutomationTriggerType.OnTicketUpdated:
        return 'badge bg-info-subtle text-info border border-info';
      case AutomationTriggerType.OnCommentAdded:
        return 'badge bg-success-subtle text-success border border-success';
      case AutomationTriggerType.ScheduledTime:
        return 'badge bg-warning-subtle text-warning border border-warning';
      default:
        return 'badge bg-secondary';
    }
  }

  getTriggerLabel(trigger: AutomationTriggerType): string {
    switch (trigger) {
      case AutomationTriggerType.OnTicketCreated:
        return '⚡ Khi tạo vé mới';
      case AutomationTriggerType.OnTicketUpdated:
        return '🔄 Khi đổi trạng thái';
      case AutomationTriggerType.OnCommentAdded:
        return '💬 Khi có phản hồi mới';
      case AutomationTriggerType.ScheduledTime:
        return '⏰ Quét theo lịch (Time-based)';
      default:
        return 'Chưa rõ';
    }
  }
}
