using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Tickets.Dtos;

public class TicketActivityDto : CreationAuditedEntityDto<Guid>
{
    public Guid TicketId { get; set; }

    public TicketActivityType ActivityType { get; set; }

    public string? FieldName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Description { get; set; }

    public string? CreatorName { get; set; }
}
