import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { TicketStatusDto, TicketStatusGetListInput, CreateUpdateTicketStatusDto, StatusGroup } from '../../proxy/helpdesk/models';
import { TicketStatusService } from '../../proxy/helpdesk/ticket-status.service';

@Component({
  selector: 'app-ticket-statuses',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './ticket-statuses.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketStatusesComponent implements OnInit {
  private svc = inject(TicketStatusService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<TicketStatusGetListInput>);

  items: TicketStatusDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';

  readonly StatusGroup = StatusGroup;
  readonly statusGroups = [
    { value: StatusGroup.Open, label: 'Mở (Open)' },
    { value: StatusGroup.InProgress, label: 'Đang xử lý (In Progress)' },
    { value: StatusGroup.Closed, label: 'Đã đóng (Closed)' },
  ];

  getGroupLabel(group: StatusGroup): string {
    return this.statusGroups.find(g => g.value === group)?.label ?? '—';
  }

  getGroupBadgeClass(group: StatusGroup): string {
    switch (group) {
      case StatusGroup.Open: return 'bg-info-subtle text-info border border-info-subtle';
      case StatusGroup.InProgress: return 'bg-warning-subtle text-warning border border-warning-subtle';
      case StatusGroup.Closed: return 'bg-secondary-subtle text-secondary border border-secondary-subtle';
      default: return 'bg-light text-muted';
    }
  }

  form: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(500)],
    group: [StatusGroup.Open, Validators.required],
    colorHex: ['#6c757d', Validators.required],
    sortOrder: [0],
    isDefault: [false],
    isFinal: [false],
    isActive: [true],
  });

  ngOnInit(): void {
    const streamCreator = (query: TicketStatusGetListInput) =>
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
    this.form.reset({ isActive: true, isDefault: false, isFinal: false, group: StatusGroup.Open, colorHex: '#6c757d', sortOrder: 0 });
    this.isModalOpen = true;
  }

  openEditModal(item: TicketStatusDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    this.form.patchValue(item);
    this.isModalOpen = true;
  }

  closeModal(): void { this.isModalOpen = false; }

  save(): void {
    if (this.form.invalid) return;
    const input: CreateUpdateTicketStatusDto = { ...this.form.value, group: Number(this.form.value.group) };
    const req = this.isEditing ? this.svc.update(this.selectedId!, input) : this.svc.create(input);
    req.subscribe(() => { this.isModalOpen = false; this.list.get(); });
  }

  delete(item: TicketStatusDto): void {
    this.confirmation.warn('::AreYouSureToDelete', '::AreYouSure', { messageLocalizationParams: [item.name] })
      .subscribe((s: Confirmation.Status) => {
        if (s === Confirmation.Status.confirm) this.svc.delete(item.id).subscribe(() => this.list.get());
      });
  }
}
