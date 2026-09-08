import type { StatusGroup } from '../categories/status-group.enum';
import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateTicketStatusDto {
  name: string;
  code: string;
  color?: string | null;
  isFinal?: boolean;
  isDefault?: boolean;
  statusGroup?: StatusGroup;
  order?: number;
}

export interface TicketStatusDto extends FullAuditedEntityDto<string> {
  name?: string;
  code?: string;
  color?: string | null;
  isFinal?: boolean;
  isDefault?: boolean;
  statusGroup?: StatusGroup;
  order?: number;
}

export interface TicketStatusGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
}
