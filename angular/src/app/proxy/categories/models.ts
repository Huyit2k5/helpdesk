import type { EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CategoryDto extends FullAuditedEntityDto<string> {
  name?: string;
  code?: string;
  parentId?: string | null;
  parentName?: string | null;
  description?: string | null;
  isActive?: boolean;
  order?: number;
}

export interface CategoryGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  parentId?: string | null;
  isActive?: boolean | null;
}

export interface CategoryLookupDto extends EntityDto<string> {
  name?: string;
}

export interface CreateUpdateCategoryDto {
  name: string;
  code: string;
  parentId?: string | null;
  description?: string | null;
  isActive?: boolean;
  order?: number;
}
