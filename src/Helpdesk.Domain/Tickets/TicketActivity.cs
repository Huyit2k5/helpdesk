using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Tickets;

/// <summary>
/// Ghi vết lịch sử biến động của Ticket (dòng thời gian hoạt động).
/// </summary>
public class TicketActivity : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid TicketId { get; private set; }

    public TicketActivityType ActivityType { get; private set; }

    public string? FieldName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Description { get; set; }

    protected TicketActivity()
    {
        // For EF Core
    }

    public TicketActivity(
        Guid id,
        Guid ticketId,
        TicketActivityType activityType,
        string? fieldName = null,
        string? oldVal = null,
        string? newVal = null,
        string? description = null)
        : base(id)
    {
        TicketId = ticketId;
        ActivityType = activityType;
        FieldName = fieldName;
        OldValue = oldVal;
        NewValue = newVal;
        Description = description;
    }
}
