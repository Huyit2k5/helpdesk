import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export enum AutomationTriggerType {
  OnTicketCreated = 1,
  OnTicketUpdated = 2,
  OnCommentAdded = 3,
  ScheduledTime = 4,
}

export enum ConditionField {
  Status = 1,
  Priority = 2,
  Category = 3,
  Department = 4,
  Assignee = 5,
  Source = 6,
  Title = 7,
  Description = 8,
  HoursSinceLastUpdate = 9,
  HoursSinceCreated = 10,
  IsUnassigned = 11,
  Tags = 12,
}

export enum ConditionOperator {
  Equals = 1,
  NotEquals = 2,
  Contains = 3,
  NotContains = 4,
  GreaterThan = 5,
  LessThan = 6,
  IsEmpty = 7,
  IsNotEmpty = 8,
}

export enum AutomationActionType {
  ChangeStatus = 1,
  ChangePriority = 2,
  AssignToUser = 3,
  AssignToDepartment = 4,
  AddTags = 5,
  AddComment = 6,
  SendDiscordAlert = 7,
  SendEmail = 8,
}

export interface RuleCondition {
  field: ConditionField;
  operator: ConditionOperator;
  value?: string;
}

export interface RuleAction {
  actionType: AutomationActionType;
  targetValue?: string;
  additionalValue?: string;
}

export interface AutomationRuleDto extends FullAuditedEntityDto<string> {
  name: string;
  description?: string;
  triggerType: AutomationTriggerType;
  executionOrder: number;
  isActive: boolean;
  stopProcessing: boolean;
  conditions: RuleCondition[];
  actions: RuleAction[];
}

export interface CreateUpdateAutomationRuleDto {
  name: string;
  description?: string;
  triggerType: AutomationTriggerType;
  executionOrder: number;
  isActive: boolean;
  stopProcessing: boolean;
  conditions: RuleCondition[];
  actions: RuleAction[];
}

export interface GetAutomationRuleListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  triggerType?: AutomationTriggerType;
  isActive?: boolean;
}

export interface MacroDto extends FullAuditedEntityDto<string> {
  name: string;
  description?: string;
  order: number;
  isActive: boolean;
  actions: RuleAction[];
}

export interface CreateUpdateMacroDto {
  name: string;
  description?: string;
  order: number;
  isActive: boolean;
  actions: RuleAction[];
}

export interface GetMacroListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  isActive?: boolean;
}
