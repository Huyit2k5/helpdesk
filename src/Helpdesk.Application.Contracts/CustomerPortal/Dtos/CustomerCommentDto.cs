using System;
using System.Collections.Generic;
using Helpdesk.Tickets.Dtos;

namespace Helpdesk.CustomerPortal.Dtos;

public class CustomerCommentDto
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
    public string CreatorName { get; set; } = null!;
    public bool IsFromSupport { get; set; }
    public List<TicketAttachmentDto> Attachments { get; set; } = new();
}
