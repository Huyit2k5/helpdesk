using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Tickets.Dtos;

public class TicketCommentDto : FullAuditedEntityDto<Guid>
{
    public Guid TicketId { get; set; }

    public string Content { get; set; } = null!;

    public bool IsInternal { get; set; }

    public string? CreatorName { get; set; }

    public List<TicketAttachmentDto> Attachments { get; set; } = new();
}

public class CreateTicketCommentDto
{
    public string Content { get; set; } = null!;

    public bool IsInternal { get; set; }
}
