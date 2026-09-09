using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.AssignmentRules.Dtos;

public class AssignmentRuleDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public AssignmentStrategy RoutingStrategy { get; set; }
    public string RoutingStrategyName => RoutingStrategy switch
    {
        AssignmentStrategy.RoundRobin => "Xoay vòng (Round-Robin)",
        AssignmentStrategy.LeastBusy => "Cân bằng tải (Least Busy)",
        AssignmentStrategy.DirectAssign => "Chỉ định cố định",
        _ => RoutingStrategy.ToString()
    };

    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public Guid? PriorityId { get; set; }
    public string? PriorityName { get; set; }

    public Guid? SourceId { get; set; }
    public string? SourceName { get; set; }

    public Guid? DirectAssigneeId { get; set; }
    public string? DirectAssigneeName { get; set; }

    public Guid? LastAssignedUserId { get; set; }

    public List<AssignmentRuleAgentDto> Agents { get; set; } = new();
}
