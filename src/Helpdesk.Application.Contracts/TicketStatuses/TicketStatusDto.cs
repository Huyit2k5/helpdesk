using System;
using Helpdesk.Categories;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.TicketStatuses;

public class TicketStatusDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string? Color { get; set; }
    public bool IsFinal { get; set; }
    public bool IsDefault { get; set; }
    public StatusGroup StatusGroup { get; set; }
    public int Order { get; set; }
}
