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
  assignedToMeOnly = false;
  dateFrom = '';
  dateTo = '';
  isExporting = false;

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
        assignedToMe: this.assignedToMeOnly || undefined,
        dateFrom: this.dateFrom ? new Date(this.dateFrom).toISOString() : undefined,
        dateTo: this.dateTo ? new Date(this.dateTo + 'T23:59:59').toISOString() : undefined,
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
    this.assignedToMeOnly = false;
    this.dateFrom = '';
    this.dateTo = '';
    this.list.get();
  }

  toggleAssignedToMe(): void {
    this.assignedToMeOnly = !this.assignedToMeOnly;
    if (this.assignedToMeOnly) {
      this.selectedAssigneeId = '';
    }
    this.search();
  }

  exportExcel(): void {
    this.isExporting = true;
    const input: GetTicketListInput = {
      filter: this.filterText || undefined,
      statusId: this.selectedStatusId || undefined,
      priorityId: this.selectedPriorityId || undefined,
      categoryId: this.selectedCategoryId || undefined,
      departmentId: this.selectedDepartmentId || undefined,
      assigneeId: this.selectedAssigneeId || undefined,
      assignedToMe: this.assignedToMeOnly || undefined,
      dateFrom: this.dateFrom ? new Date(this.dateFrom).toISOString() : undefined,
      dateTo: this.dateTo ? new Date(this.dateTo + 'T23:59:59').toISOString() : undefined,
      maxResultCount: 1000,
      skipCount: 0,
    };

    this.ticketSvc.exportExcel(input).subscribe({
      next: (blob: Blob) => {
        this.isExporting = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Danh_Sach_Su_Vu_${new Date().toISOString().slice(0, 10)}.xlsx`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
        this.toaster.success('Xuất file Excel thành công!', 'Thành công');
        this.cdr.markForCheck();
      },
      error: () => {
        this.isExporting = false;
        this.toaster.error('Lỗi khi xuất file Excel', 'Thất bại');
        this.cdr.markForCheck();
      }
    });
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

  getSlaStatus(item: TicketListDto): { text: string; cssClass: string; icon: string } {
    if (item.isResolutionBreached) {
      return { text: 'Trễ hạn giải quyết', cssClass: 'bg-danger-subtle text-danger border border-danger-subtle', icon: 'fas fa-exclamation-circle' };
    }
    if (item.isFirstResponseBreached) {
      return { text: 'Trễ phản hồi đầu', cssClass: 'bg-warning-subtle text-warning-emphasis border border-warning-subtle', icon: 'fas fa-exclamation-triangle' };
    }
    if (item.resolvedAt) {
      return { text: 'Đúng hạn SLA', cssClass: 'bg-success-subtle text-success border border-success-subtle', icon: 'fas fa-check-circle' };
    }
    if (item.dueDate) {
      const now = new Date().getTime();
      const due = new Date(item.dueDate).getTime();
      const diffMinutes = Math.floor((due - now) / (1000 * 60));
      if (diffMinutes < 0) {
        return { text: 'Quá hạn xử lý', cssClass: 'bg-danger text-white', icon: 'fas fa-clock' };
      }
      if (diffMinutes <= 120) {
        return { text: `Còn ${diffMinutes}p`, cssClass: 'bg-warning text-dark', icon: 'fas fa-hourglass-half' };
      }
      return { text: 'Trong hạn', cssClass: 'bg-primary-subtle text-primary border border-primary-subtle', icon: 'fas fa-stopwatch' };
    }
    return { text: '—', cssClass: 'text-muted small', icon: '' };
  }
}
