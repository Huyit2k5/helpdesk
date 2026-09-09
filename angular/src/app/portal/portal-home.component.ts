import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '@abp/ng.core';
import { CustomerPortalService, CustomerTicketDto } from '../proxy/customer-portal';
import { KnowledgeArticleService, KnowledgeArticleDto } from '../proxy/knowledge-base';

@Component({
  selector: 'app-portal-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './portal-home.component.html',
  styleUrls: ['./portal-home.component.scss']
})
export class PortalHomeComponent implements OnInit {
  private portalService = inject(CustomerPortalService);
  private kbService = inject(KnowledgeArticleService);
  private authService = inject(AuthService);

  recentTickets: CustomerTicketDto[] = [];
  popularArticles: KnowledgeArticleDto[] = [];
  isLoading = true;

  get userName(): string {
    return 'Bạn';
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;

    this.portalService.getMyTickets({ maxResultCount: 3, skipCount: 0 }).subscribe({
      next: (res) => {
        this.recentTickets = res.items || [];
      }
    });

    this.kbService.getPopularArticles(4).subscribe({
      next: (res) => {
        this.popularArticles = res || [];
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}
