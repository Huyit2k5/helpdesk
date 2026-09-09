import type { EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { TicketAttachmentDto } from '../tickets/dtos/models';

export interface CustomerTicketDto extends EntityDto<string> {
  ticketNumber: string;
  title: string;
  categoryName: string;
  priorityName: string;
  priorityColor: string;
  statusName: string;
  statusColor: string;
  isFinal: boolean;
  creationTime: string;
  lastModificationTime?: string;
  commentCount: number;
  csatRating?: number;
}

export interface GetCustomerTicketListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  categoryId?: string;
  isClosed?: boolean;
}

export interface CreateCustomerTicketDto {
  title: string;
  description: string;
  categoryId: string;
  priorityId?: string;
  attachments?: CreateAttachmentInput[];
}

export interface CreateAttachmentInput {
  fileName: string;
  fileSize: number;
  contentType: string;
  blobName: string;
}

export interface CustomerCommentDto {
  id: string;
  content: string;
  creationTime: string;
  creatorId?: string;
  creatorName: string;
  isFromSupport: boolean;
  attachments: TicketAttachmentDto[];
}

export interface CustomerTicketDetailDto extends EntityDto<string> {
  ticketNumber: string;
  title: string;
  description: string;
  categoryId: string;
  categoryName: string;
  priorityId: string;
  priorityName: string;
  priorityColor: string;
  statusId: string;
  statusName: string;
  statusColor: string;
  isFinal: boolean;
  creationTime: string;
  lastModificationTime?: string;
  dueDate?: string;
  resolvedAt?: string;
  csatRating?: number;
  csatComment?: string;
  csatSubmittedAt?: string;
  attachments: TicketAttachmentDto[];
  comments: CustomerCommentDto[];
}

export interface AddCustomerCommentDto {
  content: string;
  attachments?: CreateAttachmentInput[];
}

export interface SubmitTicketFeedbackDto {
  rating: number;
  comment?: string;
}
