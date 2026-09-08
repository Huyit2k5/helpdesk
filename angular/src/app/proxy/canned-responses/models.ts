import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CannedResponseDto extends FullAuditedEntityDto<string> {
  title?: string;
  content?: string;
  categoryId?: string | null;
  categoryName?: string | null;
  isPublic?: boolean;
  usageCount?: number;
}

export interface CannedResponseGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  categoryId?: string | null;
  isPublic?: boolean | null;
}

export interface CreateUpdateCannedResponseDto {
  title: string;
  content: string;
  categoryId?: string | null;
  isPublic?: boolean;
}
