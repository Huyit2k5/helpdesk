import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { MacroService } from '../../proxy/automations/macro.service';
import {
  AutomationActionType,
  CreateUpdateMacroDto,
  GetMacroListInput,
  MacroDto,
  RuleAction
} from '../../proxy/automations/models';
import { TicketStatusService } from '../../proxy/ticket-statuses/ticket-status.service';
import { TicketStatusDto } from '../../proxy/ticket-statuses/models';
import { PriorityService } from '../../proxy/priorities/priority.service';
import { PriorityDto } from '../../proxy/priorities/models';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Component({
  selector: 'app-macros',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './macros.component.html',
  styleUrls: ['./macros.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MacrosComponent implements OnInit {
  private svc = inject(MacroService);
  private statusSvc = inject(TicketStatusService);
  private prioritySvc = inject(PriorityService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<GetMacroListInput>);

  items: MacroDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';

  statuses: TicketStatusDto[] = [];
  priorities: PriorityDto[] = [];
  AutomationActionType = AutomationActionType;

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(128)]],
    description: ['', [Validators.maxLength(512)]],
    order: [1, [Validators.required, Validators.min(0)]],
    isActive: [true],
    actions: this.fb.array([]),
  });

  get actionsArray(): FormArray {
    return this.form.get('actions') as FormArray;
  }

  ngOnInit(): void {
    this.loadLookups();
    const streamCreator = (query: GetMacroListInput) =>
      this.svc.getList({ ...query, filter: this.filterText });

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.items = res.items ?? [];
      this.totalCount = res.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  loadLookups(): void {
    this.statusSvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))).subscribe(d => {
      this.statuses = d.items ?? [];
      this.cdr.markForCheck();
    });
    this.prioritySvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))).subscribe(d => {
      this.priorities = d.items ?? [];
      this.cdr.markForCheck();
    });
  }

  onFilter(): void {
    this.list.get();
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.selectedId = undefined;
    this.actionsArray.clear();

    this.form.reset({
      name: '',
      description: '',
      order: this.items.length + 1,
      isActive: true,
    });

    this.addAction();
    this.isModalOpen = true;
    this.cdr.markForCheck();
  }

  openEditModal(macro: MacroDto): void {
    this.isEditing = true;
    this.selectedId = macro.id;
    this.actionsArray.clear();

    this.form.patchValue({
      name: macro.name,
      description: macro.description,
      order: macro.order,
      isActive: macro.isActive,
    });

    if (macro.actions && macro.actions.length > 0) {
      macro.actions.forEach(a => this.addAction(a));
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

  addAction(action?: RuleAction): void {
    const group = this.fb.group({
      actionType: [action?.actionType ?? AutomationActionType.AddComment, [Validators.required]],
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
    const dto: CreateUpdateMacroDto = {
      name: val.name,
      description: val.description,
      order: Number(val.order),
      isActive: Boolean(val.isActive),
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
        console.error('Lỗi khi lưu Macro:', err);
      },
    });
  }

  toggleActive(macro: MacroDto, event: Event): void {
    event.stopPropagation();
    if (!macro.id) return;
    this.svc.toggleActive(macro.id).subscribe(() => {
      macro.isActive = !macro.isActive;
      this.cdr.markForCheck();
    });
  }

  delete(macro: MacroDto): void {
    if (!macro.id) return;
    this.confirmation.warn(
      `Bạn có chắc chắn muốn xóa Macro "${macro.name}" không?`,
      'Xác nhận xóa'
    ).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.svc.delete(macro.id!).subscribe(() => {
          this.list.get();
        });
      }
    });
  }
}
