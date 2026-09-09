import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto, EntityDto } from '@abp/ng.core';

export enum AssignmentStrategy {
  RoundRobin = 1,
  LeastBusy = 2,
  DirectAssign = 3,
}

export interface AssignmentRuleAgentDto extends EntityDto<string> {
  ruleId: string;
  userId: string;
  userName?: string;
  fullName?: string;
  order: number;
}

export interface AssignmentRuleDto extends FullAuditedEntityDto<string> {
  name: string;
  description?: string;
  order: number;
  isActive: boolean;
  routingStrategy: AssignmentStrategy;
  routingStrategyName: string;
  departmentId?: string;
  departmentName?: string;
  categoryId?: string;
  categoryName?: string;
  priorityId?: string;
  priorityName?: string;
  sourceId?: string;
  sourceName?: string;
  directAssigneeId?: string;
  directAssigneeName?: string;
  lastAssignedUserId?: string;
  agents: AssignmentRuleAgentDto[];
}

export interface CreateUpdateAssignmentRuleDto {
  name: string;
  description?: string;
  order: number;
  isActive: boolean;
  routingStrategy: AssignmentStrategy;
  departmentId?: string;
  categoryId?: string;
  priorityId?: string;
  sourceId?: string;
  directAssigneeId?: string;
  agentUserIds: string[];
}

export interface AssignmentRuleGetListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
}

export interface AgentLookupDto {
  id: string;
  userName: string;
  name?: string;
  email?: string;
  displayName: string;
}
