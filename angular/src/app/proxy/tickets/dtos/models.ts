import type { CreationAuditedEntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { TicketActivityType } from '../ticket-activity-type.enum';

export interface AssignTicketInput {
  assigneeId?: string | null;
  departmentId?: string | null;
}

export interface ChangeTicketStatusInput {
  statusId: string;
  comment?: string | null;
}

export interface CreateTicketCommentDto {
  content?: string;
  isInternal?: boolean;
}

export interface CreateTicketDto {
  title: string;
  description?: string;
  categoryId: string;
  priorityId: string;
  statusId: string;
  sourceId: string;
  departmentId?: string | null;
  assigneeId?: string | null;
  requesterId?: string | null;
  requesterName: string;
  requesterEmail: string;
  requesterPhone?: string | null;
  dueDate?: string | null;
  tags?: string | null;
}

export interface GetTicketListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  statusId?: string | null;
  priorityId?: string | null;
  categoryId?: string | null;
  departmentId?: string | null;
  assigneeId?: string | null;
  sourceId?: string | null;
  dateFrom?: string | null;
  dateTo?: string | null;
}

export interface TicketActivityDto extends CreationAuditedEntityDto<string> {
  ticketId?: string;
  activityType?: TicketActivityType;
  fieldName?: string | null;
  oldValue?: string | null;
  newValue?: string | null;
  description?: string | null;
  creatorName?: string | null;
}

export interface TicketAttachmentDto {
  id?: string;
  ticketId?: string;
  commentId?: string | null;
  fileName?: string;
  fileSize?: number;
  contentType?: string;
  creationTime?: string;
  creatorId?: string | null;
  creatorName?: string | null;
}

export interface TicketCommentDto extends FullAuditedEntityDto<string> {
  ticketId?: string;
  content?: string;
  isInternal?: boolean;
  creatorName?: string | null;
  attachments?: TicketAttachmentDto[];
}

export interface TicketDetailDto extends FullAuditedEntityDto<string> {
  ticketNumber?: string;
  title?: string;
  description?: string;
  categoryId?: string;
  categoryName?: string;
  priorityId?: string;
  priorityName?: string;
  priorityColor?: string | null;
  statusId?: string;
  statusName?: string;
  statusColor?: string | null;
  statusGroup?: number;
  isFinalStatus?: boolean;
  sourceId?: string;
  sourceName?: string;
  departmentId?: string | null;
  departmentName?: string | null;
  assigneeId?: string | null;
  assigneeName?: string | null;
  requesterId?: string | null;
  requesterName?: string;
  requesterEmail?: string;
  requesterPhone?: string | null;
  dueDate?: string | null;
  resolvedAt?: string | null;
  closedAt?: string | null;
  slaPolicyId?: string | null;
  slaPolicyName?: string | null;
  firstResponseDueDate?: string | null;
  firstRespondedAt?: string | null;
  isFirstResponseBreached?: boolean;
  isResolutionBreached?: boolean;
  tags?: string | null;
  comments?: TicketCommentDto[];
  activities?: TicketActivityDto[];
  attachments?: TicketAttachmentDto[];
}

export interface TicketListDto extends FullAuditedEntityDto<string> {
  ticketNumber?: string;
  title?: string;
  categoryId?: string;
  categoryName?: string;
  priorityId?: string;
  priorityName?: string;
  priorityColor?: string | null;
  statusId?: string;
  statusName?: string;
  statusColor?: string | null;
  statusGroup?: number;
  sourceId?: string;
  sourceName?: string;
  departmentId?: string | null;
  departmentName?: string | null;
  assigneeId?: string | null;
  assigneeName?: string | null;
  requesterName?: string;
  requesterEmail?: string;
  dueDate?: string | null;
  resolvedAt?: string | null;
  closedAt?: string | null;
  slaPolicyId?: string | null;
  slaPolicyName?: string | null;
  firstResponseDueDate?: string | null;
  firstRespondedAt?: string | null;
  isFirstResponseBreached?: boolean;
  isResolutionBreached?: boolean;
  tags?: string | null;
  commentCount?: number;
}

export interface UpdateTicketDto {
  title: string;
  description?: string;
  categoryId: string;
  priorityId: string;
  statusId: string;
  sourceId: string;
  departmentId?: string | null;
  assigneeId?: string | null;
  requesterName: string;
  requesterEmail: string;
  requesterPhone?: string | null;
  dueDate?: string | null;
  tags?: string | null;
}
