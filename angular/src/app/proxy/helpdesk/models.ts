import type { PagedAndSortedResultRequestDto } from '@abp/ng.core';

// ==================== Category ====================
export interface CategoryDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  parentId?: string;
  parentName?: string;
  sortOrder: number;
  isActive: boolean;
  supportEmail?: string;
}

export interface CreateUpdateCategoryDto {
  code: string;
  name: string;
  description?: string;
  parentId?: string;
  sortOrder: number;
  isActive: boolean;
  supportEmail?: string;
}

export interface CategoryGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
  parentId?: string;
}

export interface CategoryLookupDto {
  id: string;
  name: string;
}

// ==================== Priority ====================
export interface PriorityDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  level: number;
  colorHex: string;
  firstResponseHours: number;
  resolutionHours: number;
  isDefault: boolean;
  isActive: boolean;
}

export interface CreateUpdatePriorityDto {
  code: string;
  name: string;
  description?: string;
  level: number;
  colorHex: string;
  firstResponseHours: number;
  resolutionHours: number;
  isDefault: boolean;
  isActive: boolean;
}

export interface PriorityGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
}

// ==================== Department ====================
export interface DepartmentDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  email?: string;
  managerId?: string;
  isActive: boolean;
}

export interface CreateUpdateDepartmentDto {
  code: string;
  name: string;
  description?: string;
  email?: string;
  managerId?: string;
  isActive: boolean;
}

export interface DepartmentGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
}

// ==================== TicketStatus ====================
export enum StatusGroup {
  Open = 1,
  InProgress = 2,
  Closed = 3,
}

export interface TicketStatusDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  group: StatusGroup;
  colorHex: string;
  sortOrder: number;
  isDefault: boolean;
  isFinal: boolean;
  isActive: boolean;
}

export interface CreateUpdateTicketStatusDto {
  code: string;
  name: string;
  description?: string;
  group: StatusGroup;
  colorHex: string;
  sortOrder: number;
  isDefault: boolean;
  isFinal: boolean;
  isActive: boolean;
}

export interface TicketStatusGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
  group?: StatusGroup;
}

// ==================== TicketSource ====================
export interface TicketSourceDto {
  id: string;
  code: string;
  name: string;
  description?: string;
  icon?: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface CreateUpdateTicketSourceDto {
  code: string;
  name: string;
  description?: string;
  icon?: string;
  isDefault: boolean;
  isActive: boolean;
}

export interface TicketSourceGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
}

// ==================== CannedResponse ====================
export interface CannedResponseDto {
  id: string;
  title: string;
  content: string;
  shortcut?: string;
  categoryId?: string;
  departmentId?: string;
  usageCount: number;
  isActive: boolean;
}

export interface CreateUpdateCannedResponseDto {
  title: string;
  content: string;
  shortcut?: string;
  categoryId?: string;
  departmentId?: string;
  isActive: boolean;
}

export interface CannedResponseGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
  categoryId?: string;
  departmentId?: string;
}
