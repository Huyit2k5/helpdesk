using System;
using Helpdesk.Categories;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.TicketStatuses;

/// <summary>
/// Trạng thái ticket (configurable). Mỗi trạng thái thuộc một StatusGroup.
/// </summary>
public class TicketStatus : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    /// <summary>
    /// Màu hiển thị (hex).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Đánh dấu trạng thái kết thúc (ticket không thể chuyển tiếp nữa).
    /// </summary>
    public bool IsFinal { get; set; }

    /// <summary>
    /// Trạng thái mặc định khi tạo ticket mới. Chỉ 1 status có thể là default.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Nhóm trạng thái: Open, InProgress, Closed.
    /// </summary>
    public StatusGroup StatusGroup { get; set; }

    public int Order { get; set; }

    protected TicketStatus()
    {
    }

    public TicketStatus(
        Guid id,
        string name,
        string code,
        StatusGroup statusGroup,
        string? color = null,
        bool isFinal = false,
        bool isDefault = false,
        int order = 0)
        : base(id)
    {
        SetName(name);
        SetCode(code);
        StatusGroup = statusGroup;
        Color = color;
        IsFinal = isFinal;
        IsDefault = isDefault;
        Order = order;
    }

    public TicketStatus SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), TicketStatusConsts.MaxNameLength);
        return this;
    }

    public TicketStatus SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), TicketStatusConsts.MaxCodeLength);
        return this;
    }
}
