using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Priorities;

/// <summary>
/// Mức độ ưu tiên của ticket, kèm thông số SLA cơ bản.
/// </summary>
public class Priority : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    /// <summary>
    /// Màu hiển thị (hex, ví dụ: #FF5733).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Thời gian SLA phản hồi (giờ).
    /// </summary>
    public int SlaResponseHours { get; set; }

    /// <summary>
    /// Thời gian SLA giải quyết (giờ).
    /// </summary>
    public int SlaResolutionHours { get; set; }

    public int Order { get; set; }

    public bool IsActive { get; set; } = true;

    protected Priority()
    {
    }

    public Priority(
        Guid id,
        string name,
        string code,
        string? color = null,
        int slaResponseHours = 0,
        int slaResolutionHours = 0,
        int order = 0,
        bool isActive = true)
        : base(id)
    {
        SetName(name);
        SetCode(code);
        Color = color;
        SlaResponseHours = slaResponseHours;
        SlaResolutionHours = slaResolutionHours;
        Order = order;
        IsActive = isActive;
    }

    public Priority SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), PriorityConsts.MaxNameLength);
        return this;
    }

    public Priority SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), PriorityConsts.MaxCodeLength);
        return this;
    }
}
