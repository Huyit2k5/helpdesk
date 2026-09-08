using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Sla;

public class SlaPolicyRule : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid SlaPolicyId { get; set; }

    public Guid PriorityId { get; set; }

    public Guid? CategoryId { get; set; }

    public int ResponseTimeMinutes { get; set; }

    public int ResolutionTimeMinutes { get; set; }

    public bool EscalationEnabled { get; set; }

    public Guid? EscalateToUserId { get; set; }

    public Guid? EscalateToDepartmentId { get; set; }

    protected SlaPolicyRule() { }

    public SlaPolicyRule(
        Guid id,
        Guid slaPolicyId,
        Guid priorityId,
        Guid? categoryId,
        int responseTimeMinutes,
        int resolutionTimeMinutes,
        bool escalationEnabled = false,
        Guid? escalateToUserId = null,
        Guid? escalateToDepartmentId = null,
        Guid? tenantId = null) : base(id)
    {
        SlaPolicyId = slaPolicyId;
        PriorityId = priorityId;
        CategoryId = categoryId;
        ResponseTimeMinutes = responseTimeMinutes;
        ResolutionTimeMinutes = resolutionTimeMinutes;
        EscalationEnabled = escalationEnabled;
        EscalateToUserId = escalateToUserId;
        EscalateToDepartmentId = escalateToDepartmentId;
        TenantId = tenantId;
    }
}
