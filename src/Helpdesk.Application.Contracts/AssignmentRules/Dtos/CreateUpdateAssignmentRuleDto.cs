using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.AssignmentRules.Dtos;

public class CreateUpdateAssignmentRuleDto
{
    [Required]
    [StringLength(AssignmentRuleConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [StringLength(AssignmentRuleConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public int Order { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public AssignmentStrategy RoutingStrategy { get; set; } = AssignmentStrategy.RoundRobin;

    public Guid? DepartmentId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? PriorityId { get; set; }

    public Guid? SourceId { get; set; }

    public Guid? DirectAssigneeId { get; set; }

    /// <summary>
    /// Danh sách ID của các kỹ thuật viên được chọn trong quy tắc.
    /// </summary>
    public List<Guid> AgentUserIds { get; set; } = new();
}
