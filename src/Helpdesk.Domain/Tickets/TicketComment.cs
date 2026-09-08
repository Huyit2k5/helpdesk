using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Tickets;

/// <summary>
/// Bình luận / Phản hồi trao đổi trên vé hỗ trợ.
/// Hỗ trợ phản hồi khách hàng (Public) và ghi chú nội bộ nhân viên (Internal note).
/// </summary>
public class TicketComment : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid TicketId { get; private set; }

    public string Content { get; private set; } = null!;

    /// <summary>
    /// true = Ghi chú nội bộ (chỉ kỹ thuật viên thấy); false = Trao đổi công khai với khách hàng.
    /// </summary>
    public bool IsInternal { get; set; }

    protected TicketComment()
    {
        // For EF Core
    }

    public TicketComment(
        Guid id,
        Guid ticketId,
        string content,
        bool isInternal = false)
        : base(id)
    {
        TicketId = ticketId;
        SetContent(content);
        IsInternal = isInternal;
    }

    public TicketComment SetContent(string content)
    {
        Content = Check.NotNullOrWhiteSpace(content, nameof(content));
        return this;
    }
}
