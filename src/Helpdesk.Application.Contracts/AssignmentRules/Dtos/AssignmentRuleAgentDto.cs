using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.AssignmentRules.Dtos;

public class AssignmentRuleAgentDto : EntityDto<Guid>
{
    public Guid RuleId { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? FullName { get; set; }
    public int Order { get; set; }
}
