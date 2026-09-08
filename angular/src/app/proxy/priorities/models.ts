import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdatePriorityDto {
  name: string;
  code: string;
  color?: string | null;
  slaResponseHours?: number;
  slaResolutionHours?: number;
  order?: number;
  isActive?: boolean;
}

export interface PriorityDto extends FullAuditedEntityDto<string> {
  name?: string;
  code?: string;
  color?: string | null;
  slaResponseHours?: number;
  slaResolutionHours?: number;
  order?: number;
  isActive?: boolean;
}

export interface PriorityGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
}
