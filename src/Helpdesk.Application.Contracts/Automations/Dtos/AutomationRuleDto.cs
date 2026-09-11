using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Automations.Dtos;

public class AutomationRuleDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public AutomationTriggerType TriggerType { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsActive { get; set; }
    public bool StopProcessing { get; set; }
    public List<RuleCondition> Conditions { get; set; } = new();
    public List<RuleAction> Actions { get; set; } = new();
}

public class CreateUpdateAutomationRuleDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public AutomationTriggerType TriggerType { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool StopProcessing { get; set; } = false;
    public List<RuleCondition> Conditions { get; set; } = new();
    public List<RuleAction> Actions { get; set; } = new();
}

public class GetAutomationRuleListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public AutomationTriggerType? TriggerType { get; set; }
    public bool? IsActive { get; set; }
}
