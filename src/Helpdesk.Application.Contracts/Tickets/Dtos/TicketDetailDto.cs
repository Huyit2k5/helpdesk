using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Tickets.Dtos;

public class TicketDetailDto : FullAuditedEntityDto<Guid>
{
    public string TicketNumber { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public Guid PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public string? PriorityColor { get; set; }

    public Guid StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? StatusColor { get; set; }
    public int StatusGroup { get; set; }
    public bool IsFinalStatus { get; set; }

    public Guid SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }

    public Guid? RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string RequesterEmail { get; set; } = string.Empty;
    public string? RequesterPhone { get; set; }

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

    public int? CsatRating { get; set; }
    public string? CsatComment { get; set; }
    public DateTime? CsatSubmittedAt { get; set; }

    public List<TicketCommentDto> Comments { get; set; } = new();

    public List<TicketActivityDto> Activities { get; set; } = new();

    public List<TicketAttachmentDto> Attachments { get; set; } = new();
}
