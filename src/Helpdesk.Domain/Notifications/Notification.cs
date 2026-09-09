using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Notifications;

/// <summary>
/// Thông báo trong-ứng-dụng gửi tới một người dùng cụ thể (kỹ thuật viên hoặc khách hàng).
/// </summary>
public class Notification : CreationAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Người nhận thông báo.
    /// </summary>
    public Guid RecipientUserId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Message { get; private set; } = null!;

    public NotificationType Type { get; private set; }

    /// <summary>
    /// Vé liên quan (nếu có) để điều hướng khi bấm vào thông báo.
    /// </summary>
    public Guid? TicketId { get; private set; }

    public bool IsRead { get; private set; }

    public DateTime? ReadTime { get; private set; }

    protected Notification()
    {
    }

    public Notification(
        Guid id,
        Guid recipientUserId,
        NotificationType type,
        string title,
        string message,
        Guid? ticketId)
        : base(id)
    {
        RecipientUserId = recipientUserId;
        Type = type;
        Title = title;
        Message = message;
        TicketId = ticketId;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadTime = DateTime.UtcNow;
    }
}
