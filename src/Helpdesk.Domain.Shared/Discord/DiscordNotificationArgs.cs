using System;
using Volo.Abp.BackgroundJobs;

namespace Helpdesk.Discord;

public enum DiscordNotificationType
{
    TicketCreated = 1,
    TicketAssigned = 2,
    SlaBreached = 3,
    TicketResolved = 4
}

[BackgroundJobName("Helpdesk.DiscordNotification")]
public class DiscordNotificationArgs
{
    public DiscordNotificationType Type { get; set; }
    public Guid TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCritical { get; set; }

    // Dành cho sự kiện phân công (Assigned)
    public string? AssigneeName { get; set; }

    // Dành cho sự kiện vi phạm SLA (SlaBreached)
    public string? BreachType { get; set; }

    // Dành cho sự kiện giải quyết vé (Resolved)
    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
