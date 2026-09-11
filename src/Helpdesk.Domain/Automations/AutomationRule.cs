using System;
using System.Collections.Generic;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Automations;

/// <summary>
/// Quy tắc tự động hóa xử lý sự vụ (Workflow Automation Rule).
/// </summary>
public class AutomationRule : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Loại sự kiện kích hoạt (Tạo vé, Cập nhật, Thêm comment, Quét theo lịch).
    /// </summary>
    public AutomationTriggerType TriggerType { get; set; }

    /// <summary>
    /// Thứ tự ưu tiên chạy quy tắc (số nhỏ hơn chạy trước).
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Trạng thái kích hoạt của quy tắc.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Dừng xử lý các quy tắc tiếp theo nếu quy tắc này đã khớp và thực thi thành công.
    /// </summary>
    public bool StopProcessing { get; set; } = false;

    /// <summary>
    /// Chuỗi JSON lưu danh sách điều kiện lọc.
    /// </summary>
    public string ConditionsJson { get; set; } = "[]";

    /// <summary>
    /// Chuỗi JSON lưu danh sách hành động thực thi.
    /// </summary>
    public string ActionsJson { get; set; } = "[]";

    protected AutomationRule()
    {
    }

    public AutomationRule(
        Guid id,
        string name,
        AutomationTriggerType triggerType,
        int executionOrder = 0,
        string? description = null,
        bool isActive = true,
        bool stopProcessing = false,
        string? conditionsJson = null,
        string? actionsJson = null,
        Guid? tenantId = null)
        : base(id)
    {
        SetName(name);
        TriggerType = triggerType;
        ExecutionOrder = executionOrder;
        Description = description;
        IsActive = isActive;
        StopProcessing = stopProcessing;
        ConditionsJson = conditionsJson ?? "[]";
        ActionsJson = actionsJson ?? "[]";
        TenantId = tenantId;
    }

    public void SetName(string name)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name), maxLength: 128);
        Name = name.Trim();
    }

    public List<RuleCondition> GetConditions()
    {
        if (string.IsNullOrWhiteSpace(ConditionsJson))
        {
            return new List<RuleCondition>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<RuleCondition>>(ConditionsJson) ?? new List<RuleCondition>();
        }
        catch
        {
            return new List<RuleCondition>();
        }
    }

    public void SetConditions(List<RuleCondition> conditions)
    {
        ConditionsJson = JsonSerializer.Serialize(conditions ?? new List<RuleCondition>());
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
