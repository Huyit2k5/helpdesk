import { Component, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TicketService } from '../../proxy/tickets/ticket.service';
import { TicketDetailDto, TicketCommentDto, TicketActivityDto } from '../../proxy/tickets/dtos/models';
import { TicketStatusService } from '../../proxy/ticket-statuses/ticket-status.service';
import { TicketStatusDto } from '../../proxy/ticket-statuses/models';
import { IdentityUserService, IdentityUserDto } from '@abp/ng.identity/proxy';
import { CannedResponseService } from '../../proxy/canned-responses/canned-response.service';
import { CannedResponseDto } from '../../proxy/canned-responses/models';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { catchError, of } from 'rxjs';

export interface TimelineItem {
  id: string;
  type: 'comment' | 'activity';
  date: string | Date;
  isInternal?: boolean;
  comment?: TicketCommentDto;
  activity?: TicketActivityDto;
}

@Component({
  selector: 'app-ticket-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
  templateUrl: './ticket-detail.component.html',
  styleUrls: ['./ticket-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TicketDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private ticketSvc = inject(TicketService);
  private statusSvc = inject(TicketStatusService);
  private userSvc = inject(IdentityUserService);
  private cannedSvc = inject(CannedResponseService);
  private toaster = inject(ToasterService);
  private confirmation = inject(ConfirmationService);
  private cdr = inject(ChangeDetectorRef);

  ticketId = '';
  ticket?: TicketDetailDto;
  isLoading = true;
  isSubmittingComment = false;

  // Active tab in composer: 'reply' = public, 'note' = internal
  composerTab: 'reply' | 'note' = 'reply';
  commentContent = '';

  // Lookups
  statuses: TicketStatusDto[] = [];
  users: IdentityUserDto[] = [];
  cannedResponses: CannedResponseDto[] = [];

  // Modals / Dropdowns
  isStatusModalOpen = false;
  isAssignModalOpen = false;
  selectedNewStatusId = '';
  statusComment = '';
  selectedNewAssigneeId: string | null = null;

  timelineItems: TimelineItem[] = [];

  ngOnInit(): void {
    this.ticketId = this.route.snapshot.paramMap.get('id') || '';
    if (this.ticketId) {
      this.loadData();
    }
  }

  loadData(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    this.ticketSvc.get(this.ticketId).subscribe({
      next: (t) => {
        this.ticket = t;
        this.buildTimeline();
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.toaster.error('Không tìm thấy thông tin sự vụ', 'Lỗi');
        this.router.navigate(['/tickets']);
        this.cdr.markForCheck();
      }
    });

    this.statusSvc.getList({ maxResultCount: 50, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.statuses = res.items ?? [];
      this.cdr.markForCheck();
    });

    this.userSvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.users = res.items ?? [];
      this.cdr.markForCheck();
    });

    this.cannedSvc.getList({ maxResultCount: 100, skipCount: 0 }).pipe(
      catchError(() => of({ items: [], totalCount: 0 }))
    ).subscribe(res => {
      this.cannedResponses = res.items ?? [];
      this.cdr.markForCheck();
    });
  }

  buildTimeline(): void {
    if (!this.ticket) return;

    const items: TimelineItem[] = [];

    (this.ticket.comments ?? []).forEach(c => {
      items.push({
        id: c.id ?? Math.random().toString(),
        type: 'comment',
        date: c.creationTime ?? '',
        isInternal: c.isInternal,
        comment: c,
      });
    });

    (this.ticket.activities ?? []).forEach(a => {
      items.push({
        id: a.id ?? Math.random().toString(),
        type: 'activity',
        date: a.creationTime ?? '',
        activity: a,
      });
    });

    // Sort ascending by time (oldest to newest for natural reading flow)
    items.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    this.timelineItems = items;
  }

  insertCannedResponse(event: Event): void {
    const selectEl = event.target as HTMLSelectElement;
    const cannedId = selectEl.value;
    if (!cannedId) return;

    const found = this.cannedResponses.find(c => c.id === cannedId);
    if (found && found.content) {
      if (this.commentContent) {
        this.commentContent += '\n\n' + found.content;
      } else {
        this.commentContent = found.content;
      }
    }
    selectEl.value = '';
    this.cdr.markForCheck();
  }

  submitComment(): void {
    if (!this.commentContent.trim() || this.isSubmittingComment || !this.ticket?.id) return;

    this.isSubmittingComment = true;
    const isInternal = this.composerTab === 'note';

    this.ticketSvc.addComment(this.ticket.id, {
      content: this.commentContent.trim(),
      isInternal: isInternal,
    }).subscribe({
      next: () => {
        this.isSubmittingComment = false;
        this.commentContent = '';
        this.toaster.success(isInternal ? 'Đã thêm ghi chú nội bộ' : 'Đã gửi phản hồi', 'Thành công');
        this.loadData();
      },
      error: () => {
        this.isSubmittingComment = false;
        this.cdr.markForCheck();
      }
    });
  }

  openChangeStatusModal(): void {
    this.selectedNewStatusId = this.ticket?.statusId || '';
    this.statusComment = '';
    this.isStatusModalOpen = true;
  }

  closeChangeStatusModal(): void {
    this.isStatusModalOpen = false;
  }

  saveStatusChange(): void {
    if (!this.ticket?.id || !this.selectedNewStatusId) return;

    this.ticketSvc.changeStatus(this.ticket.id, {
      statusId: this.selectedNewStatusId,
      comment: this.statusComment.trim() || undefined,
    }).subscribe({
      next: (updated) => {
        this.ticket = updated;
        this.isStatusModalOpen = false;
        this.toaster.success(`Đã chuyển trạng thái sang: ${updated.statusName}`, 'Thành công');
        this.loadData();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  openAssignModal(): void {
    this.selectedNewAssigneeId = this.ticket?.assigneeId || null;
    this.isAssignModalOpen = true;
  }

  closeAssignModal(): void {
    this.isAssignModalOpen = false;
  }

  saveAssign(): void {
    if (!this.ticket?.id) return;

    this.ticketSvc.assign(this.ticket.id, {
      assigneeId: this.selectedNewAssigneeId,
      departmentId: this.ticket.departmentId,
    }).subscribe({
      next: (updated) => {
        this.ticket = updated;
        this.isAssignModalOpen = false;
        this.toaster.success(
          updated.assigneeName ? `Đã phân công cho: ${updated.assigneeName}` : 'Đã hủy phân công sự vụ',
          'Thành công'
        );
        this.loadData();
      },
      error: () => {
        this.cdr.markForCheck();
      }
    });
  }

  deleteTicket(): void {
    if (!this.ticket?.id) return;

    this.confirmation.warn('Bạn có chắc chắn muốn xóa sự vụ này không?', 'Xác nhận xóa', {
      messageLocalizationParams: [this.ticket.ticketNumber ?? '']
    }).subscribe((status: Confirmation.Status) => {
      if (status === Confirmation.Status.confirm) {
        this.ticketSvc.delete(this.ticket!.id!).subscribe(() => {
          this.toaster.success('Đã xóa sự vụ thành công', 'Thành công');
          this.router.navigate(['/tickets']);
        });
      }
    });
  }
}
