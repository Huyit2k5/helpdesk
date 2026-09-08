import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { TicketSourceDto, TicketSourceGetListInput, CreateUpdateTicketSourceDto } from '../../proxy/ticket-sources/models';
import { TicketSourceService } from '../../proxy/ticket-sources/ticket-source.service';

@Component({
  selector: 'app-ticket-sources',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './ticket-sources.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketSourcesComponent implements OnInit {
  private svc = inject(TicketSourceService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<TicketSourceGetListInput>);

  items: TicketSourceDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';

  form: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    isActive: [true],
  });

  ngOnInit(): void {
    const streamCreator = (query: TicketSourceGetListInput) =>
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

  openEditModal(item: TicketSourceDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    this.form.patchValue(item);
    this.isModalOpen = true;
  }

  closeModal(): void { this.isModalOpen = false; }

  save(): void {
    if (this.form.invalid) return;
    const input: CreateUpdateTicketSourceDto = this.form.value;
    const req = this.isEditing ? this.svc.update(this.selectedId!, input) : this.svc.create(input);
    req.subscribe(() => { this.isModalOpen = false; this.list.get(); });
  }

  delete(item: TicketSourceDto): void {
    this.confirmation.warn('::AreYouSureToDelete', '::AreYouSure', { messageLocalizationParams: [item.name ?? ''] })
      .subscribe((s: Confirmation.Status) => {
        if (s === Confirmation.Status.confirm) this.svc.delete(item.id!).subscribe(() => this.list.get());
      });
  }
}
