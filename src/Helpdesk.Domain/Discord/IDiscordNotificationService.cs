using System;
using System.Threading.Tasks;
using Helpdesk.Tickets;

namespace Helpdesk.Discord;

/// <summary>
/// Dịch vụ gửi thông báo và cảnh báo sự cố qua Discord Webhook.
/// </summary>
public interface IDiscordNotificationService
{
    /// <summary>
    /// Gửi thông báo khi có vé mới được tạo (từ nhân viên hoặc khách hàng).
    /// </summary>
    Task SendTicketCreatedAsync(Ticket ticket, string categoryName, string priorityName, bool isCritical);

    /// <summary>
    /// Gửi thông báo khi vé được phân công cho kỹ thuật viên.
    /// </summary>
    Task SendTicketAssignedAsync(Ticket ticket, string assigneeName, string priorityName);

    /// <summary>
    /// Gửi cảnh báo đỏ khi vé vi phạm thời hạn SLA (phản hồi hoặc giải quyết).
    /// </summary>
    Task SendSlaBreachAsync(Ticket ticket, string breachType, DateTime dueDate, string priorityName);

    /// <summary>
    /// Gửi thông báo khi vé được xử lý hoàn tất (Resolved).
    /// </summary>
    Task SendTicketResolvedAsync(Ticket ticket, string resolvedByName);

    /// <summary>
    /// Gửi tin nhắn thử nghiệm (Test Webhook) để xác minh cấu hình URL.
    /// </summary>
    Task<bool> SendTestMessageAsync(string webhookUrl);
}
