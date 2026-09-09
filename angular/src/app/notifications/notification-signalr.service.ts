import { Injectable, inject } from '@angular/core';
import { AuthService, ConfigStateService } from '@abp/ng.core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PushedNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  ticketId?: string;
  creationTime: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationSignalrService {
  private authService = inject(AuthService);
  private configState = inject(ConfigStateService);

  private hubConnection: signalR.HubConnection | null = null;

  readonly notificationReceived$ = new Subject<PushedNotification>();

  start(): void {
    if (this.hubConnection || !this.configState.getDeep('currentUser.isAuthenticated')) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.apis.default.url}/signalr-hubs/notifications`, {
        accessTokenFactory: () => this.authService.getAccessToken(),
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('notificationReceived', (payload: PushedNotification) => {
      this.notificationReceived$.next(payload);
    });

    this.hubConnection.start().catch(err => {
      console.error('Không thể kết nối SignalR notification hub:', err);
    });
  }

  stop(): void {
    this.hubConnection?.stop();
    this.hubConnection = null;
  }
}
