using System;

namespace Helpdesk.Notifications;

/// <summary>
/// Sự kiện cục bộ phát sinh khi có 1 thông báo mới được tạo, để tầng Host (SignalR) lắng nghe và đẩy real-time
/// mà không cần tầng Domain biết gì về hạ tầng AspNetCore/SignalR.
/// </summary>
public class NotificationCreatedEto
{
    public Guid Id { get; set; }

    public Guid RecipientUserId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public Guid? TicketId { get; set; }

    public DateTime CreationTime { get; set; }
}
