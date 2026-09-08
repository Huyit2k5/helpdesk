import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ListService } from '@abp/ng.core';
import { ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { PriorityDto, PriorityGetListInput, CreateUpdatePriorityDto } from '../../proxy/helpdesk/models';
import { PriorityService } from '../../proxy/helpdesk/priority.service';

@Component({
  selector: 'app-priorities',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './priorities.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrioritiesComponent implements OnInit {
  private svc = inject(PriorityService);
  private fb = inject(FormBuilder);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<PriorityGetListInput>);

  items: PriorityDto[] = [];
  totalCount = 0;
  isModalOpen = false;
  isEditing = false;
  selectedId?: string;
  filterText = '';

  form: FormGroup = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(500)],
    level: [1, [Validators.required, Validators.min(1)]],
    colorHex: ['#FFC107', Validators.required],
    firstResponseHours: [8, [Validators.required, Validators.min(0)]],
    resolutionHours: [24, [Validators.required, Validators.min(0)]],
    isDefault: [false],
    isActive: [true],
  });

  ngOnInit(): void {
    const streamCreator = (query: PriorityGetListInput) =>
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
    this.form.reset({ isActive: true, isDefault: false, level: 1, colorHex: '#FFC107', firstResponseHours: 8, resolutionHours: 24 });
    this.isModalOpen = true;
  }

  openEditModal(item: PriorityDto): void {
    this.isEditing = true;
    this.selectedId = item.id;
    this.form.patchValue(item);
    this.isModalOpen = true;
  }

  closeModal(): void { this.isModalOpen = false; }

  save(): void {
    if (this.form.invalid) return;
    const input: CreateUpdatePriorityDto = this.form.value;
    const req = this.isEditing ? this.svc.update(this.selectedId!, input) : this.svc.create(input);
    req.subscribe(() => { this.isModalOpen = false; this.list.get(); });
  }

  delete(item: PriorityDto): void {
    this.confirmation.warn('::AreYouSureToDelete', '::AreYouSure', { messageLocalizationParams: [item.name] })
      .subscribe((s: Confirmation.Status) => {
        if (s === Confirmation.Status.confirm) this.svc.delete(item.id).subscribe(() => this.list.get());
      });
  }
}
