import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ConfigStateService, PermissionService } from '@abp/ng.core';
import { ToasterService } from '@abp/ng.theme.shared';
import { Subscription } from 'rxjs';
import { NotificationDto, NotificationService } from '../proxy/notifications';
import { NotificationSignalrService } from './notification-signalr.service';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
  styleUrls: ['./notification-bell.component.scss'],
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  private notificationService = inject(NotificationService);
  private signalr = inject(NotificationSignalrService);
  private toaster = inject(ToasterService);
  private configState = inject(ConfigStateService);
  private permissionService = inject(PermissionService);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  isOpen = false;
  unreadCount = 0;
  notifications: NotificationDto[] = [];
  isLoading = false;

  private subscription = new Subscription();
  private pollHandle: ReturnType<typeof setInterval> | null = null;

  get isAuthenticated(): boolean {
    return !!this.configState.getDeep('currentUser.isAuthenticated');
  }

  ngOnInit(): void {
    if (!this.isAuthenticated) {
      return;
    }

    this.signalr.start();
    this.refreshUnreadCount();

    this.subscription.add(
      this.signalr.notificationReceived$.subscribe(payload => {
        this.unreadCount++;
        this.toaster.info(payload.message, payload.title);
        if (this.isOpen) {
          this.loadNotifications();
        }
        this.cdr.detectChanges();
      })
    );

    // Dự phòng khi mất kết nối SignalR: đồng bộ lại số chưa đọc mỗi 60s.
    this.pollHandle = setInterval(() => this.refreshUnreadCount(), 60000);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    if (this.pollHandle) {
      clearInterval(this.pollHandle);
    }
  }

  toggle(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      this.loadNotifications();
    }
  }

  refreshUnreadCount(): void {
    this.notificationService.getUnreadCount().subscribe(count => {
      this.unreadCount = count;
      this.cdr.detectChanges();
    });
  }

  loadNotifications(): void {
    this.isLoading = true;
    this.notificationService.getMyNotifications({ maxResultCount: 20, skipCount: 0 }).subscribe(res => {
      this.notifications = res.items || [];
      this.isLoading = false;
      this.cdr.detectChanges();
    });
  }

  onNotificationClick(notification: NotificationDto): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe(() => {
        notification.isRead = true;
        this.unreadCount = Math.max(0, this.unreadCount - 1);
        this.cdr.detectChanges();
      });
    }

    this.isOpen = false;

    if (notification.ticketId) {
      const isAgentSide = this.permissionService.getGrantedPolicy('Helpdesk.Tickets');
      this.router.navigate([isAgentSide ? `/tickets/${notification.ticketId}` : `/portal/tickets/${notification.ticketId}`]);
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe(() => {
      this.notifications.forEach(n => (n.isRead = true));
      this.unreadCount = 0;
      this.cdr.detectChanges();
    });
  }
}
