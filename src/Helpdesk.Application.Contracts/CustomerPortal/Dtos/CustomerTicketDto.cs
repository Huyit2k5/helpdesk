using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.CustomerPortal.Dtos;

public class CustomerTicketDto : EntityDto<Guid>
{
    public string TicketNumber { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string PriorityName { get; set; } = null!;
    public string PriorityColor { get; set; } = "#3b82f6";
    public string StatusName { get; set; } = null!;
    public string StatusColor { get; set; } = "#10b981";
    public bool IsFinal { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public int CommentCount { get; set; }
    public int? CsatRating { get; set; }
}
