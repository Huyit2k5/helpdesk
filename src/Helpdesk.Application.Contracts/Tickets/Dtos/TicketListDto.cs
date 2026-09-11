using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Tickets.Dtos;

public class TicketListDto : FullAuditedEntityDto<Guid>
{
    public string TicketNumber { get; set; } = null!;

    public string Title { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public Guid PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string? PriorityColor { get; set; }

    public Guid StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? StatusColor { get; set; }
    public int StatusGroup { get; set; }

    public Guid SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }

    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Guid? SlaPolicyId { get; set; }
    public string? SlaPolicyName { get; set; }
    public DateTime? FirstResponseDueDate { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public bool IsFirstResponseBreached { get; set; }
    public bool IsResolutionBreached { get; set; }

    public string? Tags { get; set; }
    public Helpdesk.Ai.CustomerSentiment? AiSentiment { get; set; }
    public int CommentCount { get; set; }
}
