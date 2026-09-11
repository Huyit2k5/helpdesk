using System;
using System.Collections.Generic;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Automations;

/// <summary>
/// Đại diện cho một mẫu thao tác nhanh 1-Click (Macro) dành cho kỹ thuật viên.
/// </summary>
public class Macro : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Chuỗi JSON lưu danh sách hành động áp dụng vào vé.
    /// </summary>
    public string ActionsJson { get; set; } = "[]";

    protected Macro()
    {
    }

    public Macro(
        Guid id,
        string name,
        string? description = null,
        int order = 0,
        bool isActive = true,
        string? actionsJson = null,
        Guid? tenantId = null)
        : base(id)
    {
        SetName(name);
        Description = description;
        Order = order;
        IsActive = isActive;
        ActionsJson = actionsJson ?? "[]";
        TenantId = tenantId;
    }

    public void SetName(string name)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        Name = name.Trim();
    }

    public List<RuleAction> GetActions()
    {
        if (string.IsNullOrWhiteSpace(ActionsJson))
        {
            return new List<RuleAction>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<RuleAction>>(ActionsJson) ?? new List<RuleAction>();
        }
        catch
        {
            return new List<RuleAction>();
        }
    }

    public void SetActions(List<RuleAction> actions)
    {
        ActionsJson = JsonSerializer.Serialize(actions ?? new List<RuleAction>());
    }
}
