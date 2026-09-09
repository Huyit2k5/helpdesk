import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { CustomerPortalService, CustomerTicketDetailDto } from '../proxy/customer-portal';
import { TicketAttachmentDto } from '../proxy/tickets/dtos/models';

@Component({
  selector: 'app-ticket-view',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './ticket-view.component.html',
  styleUrls: ['./ticket-view.component.scss']
})
export class TicketViewComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private portalService = inject(CustomerPortalService);
  private cdr = inject(ChangeDetectorRef);

  ticketId!: string;
  ticket: CustomerTicketDetailDto | null = null;
  isLoading = true;

  replyContent = '';
  isSubmittingReply = false;
  selectedReplyFiles: File[] = [];

  imageBlobUrls: { [attachmentId: string]: string } = {};
  previewModalUrl: string | null = null;
  previewModalTitle = '';

  getImageUrl(attachmentId?: string): string | null {
    return attachmentId ? this.imageBlobUrls[attachmentId] || null : null;
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.ticketId = id;
        this.loadTicket();
      }
    });
  }

  loadTicket(): void {
    this.isLoading = true;
    this.portalService.getMyTicket(this.ticketId).subscribe({
      next: (res) => {
        this.ticket = res;
        this.isLoading = false;
        this.loadAllImageThumbnails();
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  private loadAllImageThumbnails(): void {
    if (!this.ticket) return;

    const allAttachments: TicketAttachmentDto[] = [
      ...(this.ticket.attachments || []),
      ...(this.ticket.comments || []).flatMap(c => c.attachments || [])
    ];

    allAttachments.forEach(att => {
      const attId = att.id;
      if (attId && this.isImageFile(att.fileName) && !this.imageBlobUrls[attId]) {
        this.portalService.downloadMyAttachment(attId).subscribe({
          next: (blob: Blob) => {
            this.imageBlobUrls[attId] = window.URL.createObjectURL(blob);
            this.cdr.detectChanges();
          },
          error: () => {
            // silent fail for image thumbnail
          }
        });
      }
    });
  }

  onReplyFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const maxSizeBytes = 10 * 1024 * 1024;
    for (let i = 0; i < input.files.length; i++) {
      const file = input.files[i];
      if (file.size > maxSizeBytes) {
        alert(`Tệp "${file.name}" vượt quá dung lượng cho phép (tối đa 10 MB).`);
        continue;
      }
      this.selectedReplyFiles.push(file);
    }
    input.value = '';
    this.cdr.detectChanges();
  }

  removeReplyFile(index: number): void {
    this.selectedReplyFiles.splice(index, 1);
    this.cdr.detectChanges();
  }

  formatBytes(bytes?: number): string {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  isImageFile(fileName?: string): boolean {
    if (!fileName) return false;
    const ext = fileName.split('.').pop()?.toLowerCase();
    return ['png', 'jpg', 'jpeg', 'gif', 'webp', 'bmp', 'svg'].includes(ext || '');
  }

  sendReply(): void {
    if ((!this.replyContent.trim() && this.selectedReplyFiles.length === 0) || this.isSubmittingReply) return;

    this.isSubmittingReply = true;
    const commentText = this.replyContent.trim() || 'Đã đính kèm hình ảnh / tệp tin minh họa.';

    this.portalService.addMyComment(this.ticketId, { content: commentText }).subscribe({
      next: (newComment) => {
        if (this.selectedReplyFiles.length > 0) {
          const uploads$ = this.selectedReplyFiles.map(file =>
            this.portalService.uploadMyAttachment(this.ticketId, file, newComment.id)
          );

          forkJoin(uploads$).subscribe({
            next: () => {
              this.selectedReplyFiles = [];
              this.replyContent = '';
              this.isSubmittingReply = false;
              this.loadTicket();
            },
            error: () => {
              this.selectedReplyFiles = [];
              this.replyContent = '';
              this.isSubmittingReply = false;
              this.loadTicket();
            }
          });
        } else {
          if (this.ticket) {
            this.ticket.comments.push(newComment);
          }
          this.replyContent = '';
          this.isSubmittingReply = false;
          this.cdr.detectChanges();
        }
      },
      error: () => {
        this.isSubmittingReply = false;
        this.cdr.detectChanges();
      }
    });
  }

  downloadAttachment(attachmentId?: string, fileName?: string): void {
    if (!attachmentId) return;

    this.portalService.downloadMyAttachment(attachmentId).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName || 'attachment';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        alert('Không thể tải tệp tin về máy.');
      }
    });
  }

  openImagePreview(attachmentId?: string, fileName?: string): void {
    if (!attachmentId) return;
    this.previewModalTitle = fileName || 'Hình ảnh đính kèm';
    if (this.imageBlobUrls[attachmentId]) {
      this.previewModalUrl = this.imageBlobUrls[attachmentId];
      this.cdr.detectChanges();
    } else {
      this.portalService.downloadMyAttachment(attachmentId).subscribe({
        next: (blob: Blob) => {
          const url = window.URL.createObjectURL(blob);
          this.imageBlobUrls[attachmentId] = url;
          this.previewModalUrl = url;
          this.cdr.detectChanges();
        }
      });
    }
  }

  closeImagePreview(): void {
    this.previewModalUrl = null;
    this.previewModalTitle = '';
    this.cdr.detectChanges();
  }
}
