using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Tickets;

/// <summary>
/// Tệp tin đính kèm vào ticket hoặc vào bình luận.
/// </summary>
public class TicketAttachment : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid TicketId { get; private set; }

    public Guid? CommentId { get; set; }

    public string FileName { get; private set; } = null!;

    public long FileSize { get; private set; }

    public string ContentType { get; private set; } = null!;

    public string BlobName { get; private set; } = null!;

    protected TicketAttachment()
    {
        // For EF Core
    }

    public TicketAttachment(
        Guid id,
        Guid ticketId,
        string fileName,
        long fileSize,
        string contentType,
        string blobName,
        Guid? commentId = null)
        : base(id)
    {
        TicketId = ticketId;
        CommentId = commentId;
        FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName), TicketConsts.MaxAttachmentFileNameLength);
        FileSize = fileSize;
        ContentType = Check.NotNullOrWhiteSpace(contentType, nameof(contentType), TicketConsts.MaxAttachmentContentTypeLength);
        BlobName = Check.NotNullOrWhiteSpace(blobName, nameof(blobName));
    }
}
