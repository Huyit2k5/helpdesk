namespace Helpdesk.Notifications;

/// <summary>
/// Loại thông báo phát sinh trong hệ thống.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Vé được phân công (tự động hoặc thủ công) cho kỹ thuật viên.
    /// </summary>
    TicketAssigned = 1,

    /// <summary>
    /// Trạng thái vé thay đổi.
    /// </summary>
    StatusChanged = 2,

    /// <summary>
    /// Có phản hồi/bình luận mới trên vé.
    /// </summary>
    CommentAdded = 3,

    /// <summary>
    /// Vé vi phạm cam kết SLA.
    /// </summary>
    SlaBreached = 4
}
