using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Sla.Dtos;

public class SlaPolicyRuleDto : EntityDto<Guid>
{
    public Guid SlaPolicyId { get; set; }
    public Guid PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
}

public class CreateUpdateSlaPolicyRuleDto
{
    public Guid? Id { get; set; }
    public Guid PriorityId { get; set; }
    public Guid? CategoryId { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
}
