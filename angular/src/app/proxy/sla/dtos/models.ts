import type { CreationAuditedEntityDto, EntityDto, FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';
import type { SlaBreachType } from '../sla-breach-type.enum';

export interface BusinessHourDto extends EntityDto<string> {
  dayOfWeek?: any;
  dayOfWeekName?: string;
  startTime?: string;
  endTime?: string;
  isWorkingDay?: boolean;
}

export interface CreateUpdateHolidayDto {
  name: string;
  date: string;
  isRecurring?: boolean;
}

export interface CreateUpdateSlaPolicyDto {
  name: string;
  description?: string | null;
  isDefault?: boolean;
  isActive?: boolean;
  rules?: CreateUpdateSlaPolicyRuleDto[];
}

export interface CreateUpdateSlaPolicyRuleDto {
  id?: string | null;
  priorityId?: string;
  categoryId?: string | null;
  responseTimeMinutes?: number;
  resolutionTimeMinutes?: number;
}

export interface GetSlaBreachListInput extends PagedAndSortedResultRequestDto {
  breachType?: SlaBreachType | null;
  startDate?: string | null;
  endDate?: string | null;
}

export interface GetSlaPolicyListInput extends PagedAndSortedResultRequestDto {
  filter?: string | null;
  isActive?: boolean | null;
}

export interface HolidayDto extends EntityDto<string> {
  name?: string;
  date?: string;
  isRecurring?: boolean;
}

export interface SlaBreachLogDto extends CreationAuditedEntityDto<string> {
  ticketId?: string;
  ticketNumber?: string;
  ticketTitle?: string;
  breachType?: SlaBreachType;
  breachTypeName?: string;
  expectedDate?: string;
  actualDate?: string | null;
  breachedMinutes?: number;
  reason?: string | null;
}

export interface SlaComplianceStatsDto {
  totalTicketsWithSla?: number;
  firstResponseMetCount?: number;
  firstResponseBreachedCount?: number;
  firstResponseComplianceRate?: number;
  resolutionMetCount?: number;
  resolutionBreachedCount?: number;
  resolutionComplianceRate?: number;
}

export interface SlaPolicyDto extends FullAuditedEntityDto<string> {
  name?: string;
  description?: string | null;
  isDefault?: boolean;
  isActive?: boolean;
  rules?: SlaPolicyRuleDto[];
}

export interface SlaPolicyRuleDto extends EntityDto<string> {
  slaPolicyId?: string;
  priorityId?: string;
  priorityName?: string;
  categoryId?: string | null;
  categoryName?: string | null;
  responseTimeMinutes?: number;
  resolutionTimeMinutes?: number;
}

export interface UpdateBusinessHourDto {
  id?: string;
  startTime?: string;
  endTime?: string;
  isWorkingDay?: boolean;
}
