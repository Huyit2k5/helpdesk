import { Component, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { ListService } from '@abp/ng.core';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { TicketService } from '../proxy/tickets/ticket.service';
import { TicketListDto, GetTicketListInput } from '../proxy/tickets/dtos/models';
import { TicketStatusService } from '../proxy/ticket-statuses/ticket-status.service';
import { TicketStatusDto } from '../proxy/ticket-statuses/models';
import { PriorityService } from '../proxy/priorities/priority.service';
import { PriorityDto } from '../proxy/priorities/models';
import { CategoryService } from '../proxy/categories/category.service';
import { CategoryLookupDto } from '../proxy/categories/models';
import { DepartmentService } from '../proxy/departments/department.service';
import { DepartmentDto } from '../proxy/departments/models';
import { IdentityUserService, IdentityUserDto } from '@abp/ng.identity/proxy';
import { TicketCreateModalComponent } from './ticket-create-modal/ticket-create-modal.component';
import { ConfirmationService, Confirmation, ToasterService } from '@abp/ng.theme.shared';

import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-tickets',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, NgbPaginationModule, TicketCreateModalComponent],
  providers: [ListService],
  templateUrl: './tickets.component.html',
  styleUrls: ['./tickets.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketsComponent implements OnInit {
  private ticketSvc = inject(TicketService);
  private statusSvc = inject(TicketStatusService);
  private prioritySvc = inject(PriorityService);
  private categorySvc = inject(CategoryService);
  private departmentSvc = inject(DepartmentService);
  private userSvc = inject(IdentityUserService);
  private confirmation = inject(ConfirmationService);
  private toaster = inject(ToasterService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<GetTicketListInput>);

  // State
  viewMode: 'table' | 'kanban' = 'table';
  items: TicketListDto[] = [];
  totalCount = 0;
  isCreateModalOpen = false;

  // Quick Change Status Modal
  isStatusModalOpen = false;
  selectedTicket?: TicketListDto;
  selectedNewStatusId = '';

  // Lookups
  statuses: TicketStatusDto[] = [];
  priorities: PriorityDto[] = [];
  categories: CategoryLookupDto[] = [];
  departments: DepartmentDto[] = [];
  users: IdentityUserDto[] = [];

  // Filters
  filterText = '';
  selectedStatusId = '';
  selectedPriorityId = '';
  selectedCategoryId = '';
  selectedDepartmentId = '';
  selectedAssigneeId = '';

  // Kanban board columns cache
  kanbanColumns: { status: TicketStatusDto; tickets: TicketListDto[] }[] = [];

  ngOnInit(): void {
    this.loadLookups();

    const streamCreator = (query: GetTicketListInput) =>
      this.ticketSvc.getList({
        ...query,
        filter: this.filterText || undefined,
        statusId: this.selectedStatusId || undefined,
        priorityId: this.selectedPriorityId || undefined,
        categoryId: this.selectedCategoryId || undefined,
        departmentId: this.selectedDepartmentId || undefined,
        assigneeId: this.selectedAssigneeId || undefined,
      }).pipe(
        catchError(() => of({ items: [], totalCount: 0 }))
      );

    this.list.hookToQuery(streamCreator).subscribe(res => {
      this.items = res.items ?? [];
      this.totalCount = res.totalCount ?? 0;
      this.buildKanbanColumns();
      this.cdr.markForCheck();
    });
  }

  loadLookups(): void {
    this.statusSvc.getList({ maxResultCount: 50, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.statuses = (res.items ?? []).sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
      this.buildKanbanColumns();
      this.cdr.markForCheck();
    });

    this.prioritySvc.getList({ maxResultCount: 50, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.priorities = (res.items ?? []).sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
      this.cdr.markForCheck();
    });

    this.categorySvc.getLookup().pipe(
      catchError(() => of({ items: [] }))
    ).subscribe(res => {
      this.categories = res.items ?? [];
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

  buildKanbanColumns(): void {
    if (this.statuses.length === 0) return;

    this.kanbanColumns = this.statuses.map(st => ({
      status: st,
      tickets: this.items.filter(t => t.statusId === st.id),
    }));
  }

  search(): void {
    this.list.get();
  }

  resetFilters(): void {
    this.filterText = '';
    this.selectedStatusId = '';
    this.selectedPriorityId = '';
    this.selectedCategoryId = '';
    this.selectedDepartmentId = '';
    this.selectedAssigneeId = '';
    this.list.get();
  }

  switchView(mode: 'table' | 'kanban'): void {
    this.viewMode = mode;
    if (mode === 'kanban') {
      // In kanban view, fetch all items matching filter (up to 100)
      this.list.maxResultCount = 100;
    } else {
      this.list.maxResultCount = 10;
    }
    this.list.get();
  }

  openCreateModal(): void {
    this.isCreateModalOpen = true;
  }

  onTicketCreated(): void {
    this.list.get();
  }

  viewDetail(ticket: TicketListDto): void {
    if (ticket.id) {
      this.router.navigate(['/tickets', ticket.id]);
    }
  }

  openQuickStatusModal(ticket: TicketListDto, event: MouseEvent): void {
    event.stopPropagation();
    this.selectedTicket = ticket;
    this.selectedNewStatusId = ticket.statusId || '';
    this.isStatusModalOpen = true;
  }

  closeQuickStatusModal(): void {
    this.isStatusModalOpen = false;
    this.selectedTicket = undefined;
  }

  saveQuickStatus(): void {
    if (!this.selectedTicket?.id || !this.selectedNewStatusId) return;

    this.ticketSvc.changeStatus(this.selectedTicket.id, {
      statusId: this.selectedNewStatusId,
    }).subscribe({
      next: () => {
        this.toaster.success('Đã cập nhật trạng thái thành công', 'Thành công');
        this.closeQuickStatusModal();
        this.list.get();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  deleteTicket(ticket: TicketListDto, event: MouseEvent): void {
    event.stopPropagation();
    if (!ticket.id) return;

    this.confirmation.warn('Bạn có chắc chắn muốn xóa sự vụ này không?', 'Xác nhận xóa', {
      messageLocalizationParams: [ticket.ticketNumber ?? '']
    }).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.ticketSvc.delete(ticket.id!).subscribe(() => {
          this.toaster.success('Đã xóa sự vụ thành công', 'Thành công');
          this.list.get();
        });
      }
    });
  }
}
