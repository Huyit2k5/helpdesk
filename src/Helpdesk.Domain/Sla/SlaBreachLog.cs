using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Sla;

public class SlaBreachLog : CreationAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid TicketId { get; set; }

    public Guid? SlaPolicyRuleId { get; set; }

    public SlaBreachType BreachType { get; set; }

    public DateTime BreachedAt { get; set; }

    public DateTime ExpectedAt { get; set; }

    public DateTime? ResolvedOrRespondedAt { get; set; }

    public int? ElapsedMinutes { get; set; }

    public string? Description { get; set; }

    protected SlaBreachLog() { }

    public SlaBreachLog(
        Guid id,
        Guid ticketId,
        SlaBreachType breachType,
        DateTime expectedAt,
        DateTime breachedAt,
        Guid? slaPolicyRuleId = null,
        DateTime? resolvedOrRespondedAt = null,
        int? elapsedMinutes = null,
        string? description = null,
        Guid? tenantId = null) : base(id)
    {
        TicketId = ticketId;
        BreachType = breachType;
        ExpectedAt = expectedAt;
        BreachedAt = breachedAt;
        SlaPolicyRuleId = slaPolicyRuleId;
        ResolvedOrRespondedAt = resolvedOrRespondedAt;
        ElapsedMinutes = elapsedMinutes;
        Description = description;
        TenantId = tenantId;
    }
}
