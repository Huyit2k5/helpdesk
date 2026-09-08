using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Priorities;

public class PriorityDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Color { get; set; }
    public int SlaResponseHours { get; set; }
    public int SlaResolutionHours { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
}
