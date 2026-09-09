import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef,
  inject, OnInit, OnDestroy, ViewChild, AfterViewInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { EnvironmentService } from '@abp/ng.core';
import { Chart, registerables } from 'chart.js';
import { interval, Subscription } from 'rxjs';

Chart.register(...registerables);

interface DashboardStats {
  totalTickets: number;
  openTickets: number;
  inProgressTickets: number;
  closedTickets: number;
  newTicketsToday: number;
  resolvedToday: number;
  firstResponseComplianceRate: number;
  resolutionComplianceRate: number;
  slaBreachedCount: number;
  overdueTicketCount: number;
  ticketTrend: TrendItem[];
  categoryDistribution: CategoryItem[];
  agentPerformance: AgentItem[];
  recentActivities: ActivityItem[];
}

interface TrendItem {
  date: string;
  createdCount: number;
  resolvedCount: number;
  closedCount: number;
}

interface CategoryItem {
  categoryId: string;
  categoryName: string;
  ticketCount: number;
  percentage: number;
}

interface AgentItem {
  userId: string;
  userName: string;
  assignedCount: number;
  resolvedCount: number;
  avgResolutionMinutes: number;
  slaComplianceRate: number;
}

interface ActivityItem {
  ticketId: string;
  ticketNumber: string;
  ticketTitle: string;
  activityType: string;
  description: string;
  creatorName: string;
  creationTime: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  private http = inject(HttpClient);
  private env = inject(EnvironmentService);
  private cdr = inject(ChangeDetectorRef);

  @ViewChild('trendChart') trendChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('categoryChart') categoryChartRef!: ElementRef<HTMLCanvasElement>;

  stats: DashboardStats | null = null;
  loading = true;
  error = '';

  // Filters
  trendDays = 30;
  autoRefresh = true;
  private refreshSub?: Subscription;

  private trendChartInstance?: Chart;
  private categoryChartInstance?: Chart;

  private apiUrl = '';

  ngOnInit(): void {
    this.apiUrl = this.env.getEnvironment().apis?.default?.url || '';
    this.loadDashboard();

    if (this.autoRefresh) {
      this.startAutoRefresh();
    }
  }

  ngAfterViewInit(): void {
    // Charts will be rendered after data loads
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
    this.trendChartInstance?.destroy();
    this.categoryChartInstance?.destroy();
  }

  toggleAutoRefresh(): void {
    this.autoRefresh = !this.autoRefresh;
    if (this.autoRefresh) {
      this.startAutoRefresh();
    } else {
      this.stopAutoRefresh();
    }
  }

  changePeriod(days: number): void {
    this.trendDays = days;
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;
    this.error = '';
    this.cdr.markForCheck();

    const params: any = { trendDays: this.trendDays };

    this.http.get<DashboardStats>(`${this.apiUrl}/api/app/dashboard/stats`, { params })
      .subscribe({
        next: (data) => {
          this.stats = data;
          this.loading = false;
          this.cdr.markForCheck();

          // Render charts after view updates
          setTimeout(() => this.renderCharts(), 50);
        },
        error: (err) => {
          this.error = 'Không thể tải dữ liệu dashboard.';
          this.loading = false;
          this.cdr.markForCheck();
          console.error('Dashboard error:', err);
        }
      });
  }

  exportCsv(type: 'trend' | 'agents' | 'categories'): void {
    if (!this.stats) return;

    let csv = '';
    let filename = '';
    const today = new Date().toISOString().slice(0, 10).replace(/-/g, '');

    if (type === 'trend') {
      csv = 'Ngày,Tạo mới,Đã giải quyết,Đã đóng\n';
      for (const item of this.stats.ticketTrend) {
        const d = new Date(item.date).toLocaleDateString('vi-VN');
        csv += `${d},${item.createdCount},${item.resolvedCount},${item.closedCount}\n`;
      }
      filename = `ticket_trend_${today}.csv`;
    } else if (type === 'agents') {
      csv = 'Nhân viên,Được giao,Đã giải quyết,TB giải quyết (phút),SLA (%)\n';
      for (const a of this.stats.agentPerformance) {
        csv += `${a.userName},${a.assignedCount},${a.resolvedCount},${a.avgResolutionMinutes},${a.slaComplianceRate}\n`;
      }
      filename = `agent_performance_${today}.csv`;
    } else {
      csv = 'Danh mục,Số ticket,Tỷ lệ (%)\n';
      for (const c of this.stats.categoryDistribution) {
        csv += `${c.categoryName},${c.ticketCount},${c.percentage}\n`;
      }
      filename = `category_distribution_${today}.csv`;
    }

    const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.click();
    URL.revokeObjectURL(link.href);
  }

  getActivityIcon(type: string): string {
    const map: Record<string, string> = {
      'Created': 'fas fa-plus-circle text-success',
      'StatusChanged': 'fas fa-exchange-alt text-info',
      'Assigned': 'fas fa-user-check text-primary',
      'CommentAdded': 'fas fa-comment text-warning',
      'InternalNoteAdded': 'fas fa-sticky-note text-secondary',
      'Resolved': 'fas fa-check-circle text-success',
      'Closed': 'fas fa-times-circle text-danger',
      'Reopened': 'fas fa-redo text-warning',
      'PriorityChanged': 'fas fa-flag text-danger',
      'CategoryChanged': 'fas fa-folder text-info',
      'DepartmentChanged': 'fas fa-building text-primary',
    };
    return map[type] || 'fas fa-circle text-muted';
  }

  getActivityLabel(type: string): string {
    const map: Record<string, string> = {
      'Created': 'Tạo mới',
      'StatusChanged': 'Đổi trạng thái',
      'Assigned': 'Gán xử lý',
      'CommentAdded': 'Bình luận',
      'InternalNoteAdded': 'Ghi chú nội bộ',
      'Resolved': 'Đã giải quyết',
      'Closed': 'Đã đóng',
      'Reopened': 'Mở lại',
      'PriorityChanged': 'Đổi ưu tiên',
      'CategoryChanged': 'Đổi danh mục',
      'DepartmentChanged': 'Đổi phòng ban',
    };
    return map[type] || type;
  }

  formatMinutes(mins: number): string {
    if (mins < 60) return `${mins}m`;
    const h = Math.floor(mins / 60);
    const m = Math.round(mins % 60);
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
  }

  timeAgo(dateStr: string): string {
    const now = new Date();
    const d = new Date(dateStr);
    const diffMs = now.getTime() - d.getTime();
    const diffMin = Math.floor(diffMs / 60000);
    if (diffMin < 1) return 'Vừa xong';
    if (diffMin < 60) return `${diffMin} phút trước`;
    const diffHours = Math.floor(diffMin / 60);
    if (diffHours < 24) return `${diffHours} giờ trước`;
    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays} ngày trước`;
  }

  // ===== Private Methods =====

  private startAutoRefresh(): void {
    this.refreshSub = interval(60000).subscribe(() => this.loadDashboard());
  }

  private stopAutoRefresh(): void {
    this.refreshSub?.unsubscribe();
    this.refreshSub = undefined;
  }

  private renderCharts(): void {
    if (!this.stats) return;
    this.renderTrendChart();
    this.renderCategoryChart();
  }

  private renderTrendChart(): void {
    if (!this.trendChartRef?.nativeElement || !this.stats?.ticketTrend?.length) return;

    this.trendChartInstance?.destroy();

    const labels = this.stats.ticketTrend.map(t => {
      const d = new Date(t.date);
      return `${d.getDate()}/${d.getMonth() + 1}`;
    });

    this.trendChartInstance = new Chart(this.trendChartRef.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Tạo mới',
            data: this.stats.ticketTrend.map(t => t.createdCount),
            borderColor: '#6366f1',
            backgroundColor: 'rgba(99, 102, 241, 0.1)',
            fill: true,
            tension: 0.4,
            pointRadius: 2,
            pointHoverRadius: 5,
          },
          {
            label: 'Đã giải quyết',
            data: this.stats.ticketTrend.map(t => t.resolvedCount),
            borderColor: '#22c55e',
            backgroundColor: 'rgba(34, 197, 94, 0.1)',
            fill: true,
            tension: 0.4,
            pointRadius: 2,
            pointHoverRadius: 5,
          },
          {
            label: 'Đã đóng',
            data: this.stats.ticketTrend.map(t => t.closedCount),
            borderColor: '#f59e0b',
            backgroundColor: 'rgba(245, 158, 11, 0.05)',
            fill: false,
            tension: 0.4,
            pointRadius: 2,
            pointHoverRadius: 5,
            borderDash: [5, 5],
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'bottom', labels: { usePointStyle: true, padding: 16 } },
          tooltip: { mode: 'index', intersect: false }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: { stepSize: 1, precision: 0 },
            grid: { color: 'rgba(0,0,0,0.05)' }
          },
          x: { grid: { display: false } }
        },
        interaction: { mode: 'nearest', axis: 'x', intersect: false }
      }
    });
  }

  private renderCategoryChart(): void {
    if (!this.categoryChartRef?.nativeElement || !this.stats?.categoryDistribution?.length) return;

    this.categoryChartInstance?.destroy();

    const colors = [
      '#6366f1', '#22c55e', '#f59e0b', '#ef4444', '#8b5cf6',
      '#06b6d4', '#ec4899', '#14b8a6', '#f97316', '#64748b'
    ];

    this.categoryChartInstance = new Chart(this.categoryChartRef.nativeElement, {
      type: 'doughnut',
      data: {
        labels: this.stats.categoryDistribution.map(c => c.categoryName),
        datasets: [{
          data: this.stats.categoryDistribution.map(c => c.ticketCount),
          backgroundColor: colors.slice(0, this.stats.categoryDistribution.length),
          borderWidth: 2,
          borderColor: '#ffffff',
          hoverOffset: 8,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'bottom', labels: { usePointStyle: true, padding: 12, font: { size: 12 } } }
        },
        cutout: '60%'
      }
    });
  }
}
