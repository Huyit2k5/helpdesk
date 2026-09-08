import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateDepartmentDto {
  name: string;
  code: string;
  managerId?: string | null;
  description?: string | null;
  isActive?: boolean;
}

export interface DepartmentDto extends FullAuditedEntityDto<string> {
  name?: string;
  code?: string;
  managerId?: string | null;
  managerName?: string | null;
  description?: string | null;
  isActive?: boolean;
}

export interface DepartmentGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
}
