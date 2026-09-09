using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Tickets.Dtos;

public class TicketAttachmentDto : EntityDto<Guid>
{
    public Guid TicketId { get; set; }

    public Guid? CommentId { get; set; }

    public string FileName { get; set; } = null!;

    public long FileSize { get; set; }

    public string ContentType { get; set; } = null!;

    public DateTime CreationTime { get; set; }

    public Guid? CreatorId { get; set; }

    public string? CreatorName { get; set; }
}
