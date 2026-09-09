using System;

namespace Helpdesk.Notifications.Dtos;

public class NotificationDto
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public Guid? TicketId { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadTime { get; set; }

    public DateTime CreationTime { get; set; }
}
