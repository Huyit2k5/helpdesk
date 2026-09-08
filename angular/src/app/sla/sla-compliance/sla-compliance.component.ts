import { ChangeDetectionStrategy, ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ListService, PagedResultDto } from '@abp/ng.core';
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap';
import { Router } from '@angular/router';
import { SlaReportService } from '../../proxy/sla/sla-report.service';
import { SlaComplianceStatsDto, SlaBreachLogDto, GetSlaBreachListInput } from '../../proxy/sla/dtos/models';
import { SlaBreachType } from '../../proxy/sla/sla-breach-type.enum';

@Component({
  selector: 'app-sla-compliance',
  standalone: true,
  imports: [CommonModule, FormsModule, NgbPaginationModule],
  providers: [ListService],
  templateUrl: './sla-compliance.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SlaComplianceComponent implements OnInit {
  private slaReportService = inject(SlaReportService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);
  readonly list = inject(ListService<GetSlaBreachListInput>);

  stats: SlaComplianceStatsDto = {
    totalTicketsWithSla: 0,
    firstResponseMetCount: 0,
    firstResponseBreachedCount: 0,
    firstResponseComplianceRate: 100,
    resolutionMetCount: 0,
    resolutionBreachedCount: 0,
    resolutionComplianceRate: 100,
  };

  startDate?: string;
  endDate?: string;
  selectedBreachType?: SlaBreachType;

  breachLogs: SlaBreachLogDto[] = [];
  totalBreachCount = 0;
  SlaBreachType = SlaBreachType;

  getFirstResponseRate(): number {
    return this.stats.firstResponseComplianceRate ?? 100;
  }

  getResolutionRate(): number {
    return this.stats.resolutionComplianceRate ?? 100;
  }

  ngOnInit(): void {
    // Default last 30 days
    const now = new Date();
    const past = new Date();
    past.setDate(now.getDate() - 30);
    this.startDate = past.toISOString().substring(0, 10);
    this.endDate = now.toISOString().substring(0, 10);

    this.loadStats();

    const streamCreator = (query: GetSlaBreachListInput) =>
      this.slaReportService.getBreachLogs({
        ...query,
        breachType: this.selectedBreachType,
        startDate: this.startDate ? new Date(this.startDate).toISOString() : undefined,
        endDate: this.endDate ? new Date(this.endDate + 'T23:59:59').toISOString() : undefined,
      });

    this.list.hookToQuery(streamCreator).subscribe(response => {
      this.breachLogs = response.items ?? [];
      this.totalBreachCount = response.totalCount ?? 0;
      this.cdr.markForCheck();
    });
  }

  loadStats(): void {
    const sDate = this.startDate ? new Date(this.startDate).toISOString() : undefined;
    const eDate = this.endDate ? new Date(this.endDate + 'T23:59:59').toISOString() : undefined;

    this.slaReportService.getComplianceStats(sDate, eDate).subscribe(res => {
      this.stats = res;
      this.cdr.markForCheck();
    });
  }

  applyFilter(): void {
    this.loadStats();
    this.list.get();
  }

  viewTicket(ticketId?: string): void {
    if (ticketId) {
      this.router.navigate(['/tickets', ticketId]);
    }
  }

  formatMinutes(mins?: number): string {
    if (!mins) return '0 phút';
    if (mins < 60) return `${mins} phút`;
    const hours = Math.floor(mins / 60);
    const remain = mins % 60;
    return remain > 0 ? `${hours}h ${remain}m` : `${hours} giờ`;
  }
}
