using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Sla;

public class SlaPolicy : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public ICollection<SlaPolicyRule> Rules { get; private set; }

    protected SlaPolicy()
    {
        Rules = new Collection<SlaPolicyRule>();
    }

    public SlaPolicy(
        Guid id,
        string name,
        string? description = null,
        bool isDefault = false,
        bool isActive = true,
        Guid? tenantId = null) : base(id)
    {
        SetName(name);
        Description = description;
        IsDefault = isDefault;
        IsActive = isActive;
        TenantId = tenantId;
        Rules = new Collection<SlaPolicyRule>();
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), SlaConsts.MaxNameLength);
    }

    public SlaPolicyRule AddRule(
        Guid id,
        Guid priorityId,
        Guid? categoryId,
        int responseTimeMinutes,
        int resolutionTimeMinutes,
        bool escalationEnabled = false,
        Guid? escalateToUserId = null,
        Guid? escalateToDepartmentId = null)
    {
        var rule = new SlaPolicyRule(
            id,
            Id,
            priorityId,
            categoryId,
            responseTimeMinutes,
            resolutionTimeMinutes,
            escalationEnabled,
            escalateToUserId,
            escalateToDepartmentId,
            TenantId);

        Rules.Add(rule);
        return rule;
    }

    public void ClearRules()
    {
        Rules.Clear();
    }
}
