import type { PagedAndSortedResultRequestDto } from '@abp/ng.core';

export enum NotificationType {
  TicketAssigned = 1,
  StatusChanged = 2,
  CommentAdded = 3,
  SlaBreached = 4,
}

export interface NotificationDto {
  id: string;
  type: string;
  title: string;
  message: string;
  ticketId?: string;
  isRead: boolean;
  readTime?: string;
  creationTime: string;
}

export interface GetNotificationListInput extends PagedAndSortedResultRequestDto {
  isRead?: boolean;
}
