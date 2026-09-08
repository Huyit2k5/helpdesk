import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateTicketSourceDto {
  name: string;
  code: string;
  isActive?: boolean;
}

export interface TicketSourceDto extends FullAuditedEntityDto<string> {
  name?: string;
  code?: string;
  isActive?: boolean;
}

export interface TicketSourceGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
}
