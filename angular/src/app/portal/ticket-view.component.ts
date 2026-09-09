import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CustomerPortalService, CustomerTicketDetailDto } from '../proxy/customer-portal';

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
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  sendReply(): void {
    if (!this.replyContent.trim() || this.isSubmittingReply) return;

    this.isSubmittingReply = true;
    this.portalService.addMyComment(this.ticketId, { content: this.replyContent.trim() }).subscribe({
      next: (newComment) => {
        if (this.ticket) {
          this.ticket.comments.push(newComment);
        }
        this.replyContent = '';
        this.isSubmittingReply = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isSubmittingReply = false;
        this.cdr.detectChanges();
      }
    });
  }

  getDownloadUrl(attachmentId?: string): string {
    return attachmentId ? `/api/app/ticket/download-attachment/${attachmentId}` : '#';
  }
}
