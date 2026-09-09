import { Component, OnInit, inject, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TicketService } from '../../proxy/tickets/ticket.service';
import { TicketDetailDto, TicketCommentDto, TicketActivityDto, TicketAttachmentDto } from '../../proxy/tickets/dtos/models';
import { TicketStatusService } from '../../proxy/ticket-statuses/ticket-status.service';
import { TicketStatusDto } from '../../proxy/ticket-statuses/models';
import { IdentityUserService, IdentityUserDto } from '@abp/ng.identity/proxy';
import { CannedResponseService } from '../../proxy/canned-responses/canned-response.service';
import { CannedResponseDto } from '../../proxy/canned-responses/models';
import { ToasterService, ConfirmationService, Confirmation } from '@abp/ng.theme.shared';
import { catchError, forkJoin, of } from 'rxjs';

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
  selectedCommentFiles: File[] = [];

  // Direct Attachment Upload & Preview
  isUploadingDirectAttachment = false;
  previewImageUrl: string | null = null;
  previewImageTitle = '';

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
    if ((!this.commentContent.trim() && this.selectedCommentFiles.length === 0) || this.isSubmittingComment || !this.ticket?.id) return;

    this.isSubmittingComment = true;
    const isInternal = this.composerTab === 'note';
    const text = this.commentContent.trim() || (isInternal ? 'Đã thêm tệp đính kèm vào ghi chú nội bộ' : 'Đã gửi kèm tệp tin đính kèm');

    this.ticketSvc.addComment(this.ticket.id, {
      content: text,
      isInternal: isInternal,
    }).subscribe({
      next: (comment) => {
        if (comment.id && this.selectedCommentFiles.length > 0) {
          const uploads = this.selectedCommentFiles.map(f => this.ticketSvc.uploadAttachment(this.ticket!.id!, f, comment.id));
          forkJoin(uploads).pipe(
            catchError(() => of([]))
          ).subscribe(() => {
            this.isSubmittingComment = false;
            this.commentContent = '';
            this.selectedCommentFiles = [];
            this.toaster.success(isInternal ? 'Đã thêm ghi chú nội bộ' : 'Đã gửi phản hồi', 'Thành công');
            this.loadData();
          });
        } else {
          this.isSubmittingComment = false;
          this.commentContent = '';
          this.selectedCommentFiles = [];
          this.toaster.success(isInternal ? 'Đã thêm ghi chú nội bộ' : 'Đã gửi phản hồi', 'Thành công');
          this.loadData();
        }
      },
      error: () => {
        this.isSubmittingComment = false;
        this.cdr.markForCheck();
      }
    });
  }

  onComposerFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      const files = Array.from(input.files);
      for (const file of files) {
        if (file.size > 10 * 1024 * 1024) {
          this.toaster.warn(`Tệp ${file.name} vượt quá giới hạn 10 MB.`, 'Cảnh báo');
          continue;
        }
        this.selectedCommentFiles.push(file);
      }
      input.value = '';
      this.cdr.markForCheck();
    }
  }

  removeComposerFile(index: number): void {
    this.selectedCommentFiles.splice(index, 1);
    this.cdr.markForCheck();
  }

  onDirectFileUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && this.ticket?.id) {
      const files = Array.from(input.files);
      const validFiles = files.filter(f => {
        if (f.size > 10 * 1024 * 1024) {
          this.toaster.warn(`Tệp ${f.name} vượt quá giới hạn 10 MB.`, 'Cảnh báo');
          return false;
        }
        return true;
      });

      if (validFiles.length === 0) return;

      this.isUploadingDirectAttachment = true;
      this.cdr.markForCheck();

      const uploads = validFiles.map(f => this.ticketSvc.uploadAttachment(this.ticket!.id!, f));
      forkJoin(uploads).pipe(
        catchError(() => of([]))
      ).subscribe(() => {
        this.isUploadingDirectAttachment = false;
        input.value = '';
        this.toaster.success(`Đã tải lên ${validFiles.length} tệp đính kèm`, 'Thành công');
        this.loadData();
      });
    }
  }

  onAutoAssign(): void {
    if (!this.ticketId) return;
    this.ticketSvc.autoAssign(this.ticketId).subscribe({
      next: updated => {
        this.ticket = updated;
        this.buildTimeline();
        this.toaster.success('Đã tự động phân công vé thành công!', 'Thành công');
        this.cdr.markForCheck();
      },
      error: () => {
        this.toaster.error('Không tìm thấy quy tắc phân công phù hợp hoặc có lỗi xảy ra.', 'Thông báo');
      }
    });
  }

  downloadAttachment(attachment: TicketAttachmentDto): void {
    if (!attachment.id) return;
    this.ticketSvc.downloadAttachment(attachment.id).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = attachment.fileName || 'attachment';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.toaster.error('Không thể tải tệp tin', 'Lỗi');
      }
    });
  }

  previewImage(attachment: TicketAttachmentDto): void {
    if (!attachment.id) return;
    this.ticketSvc.downloadAttachment(attachment.id).subscribe({
      next: (blob: Blob) => {
        this.previewImageUrl = window.URL.createObjectURL(blob);
        this.previewImageTitle = attachment.fileName || 'Hình ảnh đính kèm';
        this.cdr.markForCheck();
      },
      error: () => {
        this.toaster.error('Không thể hiển thị ảnh xem trước', 'Lỗi');
      }
    });
  }

  closeImagePreview(): void {
    if (this.previewImageUrl) {
      window.URL.revokeObjectURL(this.previewImageUrl);
      this.previewImageUrl = null;
    }
    this.cdr.markForCheck();
  }

  deleteAttachment(attachment: TicketAttachmentDto): void {
    if (!attachment.id) return;
    this.confirmation.warn(`Bạn có chắc muốn xóa tệp "${attachment.fileName}" không?`, 'Xác nhận xóa').subscribe(status => {
      if (status === Confirmation.Status.confirm) {
        this.ticketSvc.deleteAttachment(attachment.id!).subscribe({
          next: () => {
            this.toaster.success('Đã xóa tệp đính kèm', 'Thành công');
            this.loadData();
          }
        });
      }
    });
  }

  isImage(contentType?: string): boolean {
    return !!contentType && (contentType.startsWith('image/') || contentType.includes('png') || contentType.includes('jpeg') || contentType.includes('jpg'));
  }

  getFileIcon(contentType?: string, fileName?: string): string {
    const ext = fileName ? fileName.split('.').pop()?.toLowerCase() : '';
    if (this.isImage(contentType) || ['png', 'jpg', 'jpeg', 'gif', 'svg', 'webp'].includes(ext || '')) {
      return 'fas fa-file-image text-info';
    }
    if (['pdf'].includes(ext || '') || contentType?.includes('pdf')) {
      return 'fas fa-file-pdf text-danger';
    }
    if (['doc', 'docx'].includes(ext || '') || contentType?.includes('word')) {
      return 'fas fa-file-word text-primary';
    }
    if (['xls', 'xlsx', 'csv'].includes(ext || '') || contentType?.includes('sheet') || contentType?.includes('excel')) {
      return 'fas fa-file-excel text-success';
    }
    if (['zip', 'rar', '7z', 'tar', 'gz'].includes(ext || '')) {
      return 'fas fa-file-archive text-warning';
    }
    if (['txt', 'log'].includes(ext || '')) {
      return 'fas fa-file-alt text-secondary';
    }
    return 'fas fa-file text-muted';
  }

  formatFileSize(bytes?: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
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
