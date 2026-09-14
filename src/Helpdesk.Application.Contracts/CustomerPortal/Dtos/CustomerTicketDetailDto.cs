using System;
using System.Collections.Generic;
using Helpdesk.Tickets.Dtos;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.CustomerPortal.Dtos;

public class CustomerTicketDetailDto : EntityDto<Guid>
{
    public string TicketNumber { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public Guid PriorityId { get; set; }
    public string PriorityName { get; set; } = null!;
    public string PriorityColor { get; set; } = "#3b82f6";
    public Guid StatusId { get; set; }
    public string StatusName { get; set; } = null!;
    public string StatusColor { get; set; } = "#10b981";
    public bool IsFinal { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? AssetId { get; set; }
    public string? AssetTag { get; set; }
    public string? AssetName { get; set; }
    public string? AssetTypeName { get; set; }
    public string? SerialNumber { get; set; }
    public int? CsatRating { get; set; }
    public string? CsatComment { get; set; }
    public DateTime? CsatSubmittedAt { get; set; }
    public List<TicketAttachmentDto> Attachments { get; set; } = new();
    public List<CustomerCommentDto> Comments { get; set; } = new();
}
