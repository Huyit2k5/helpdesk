using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;

namespace Helpdesk.Notifications;

public interface IEmailNotificationService
{
    Task SendTicketCreatedConfirmationAsync(
        string toEmail,
        string requesterName,
        string ticketNumber,
        string title,
        string description,
        DateTime? dueDate,
        string? portalUrl = null);

    Task SendTicketResolvedNotificationAsync(
        string toEmail,
        string requesterName,
        string ticketNumber,
        string title,
        string? resolutionNote,
        string? csatUrl = null);

    Task SendTicketAssignedNotificationAsync(
        string toEmail,
        string assigneeName,
        string ticketNumber,
        string title,
        string priorityName,
        DateTime? dueDate,
        string? ticketUrl = null);
}

public class EmailNotificationService : IEmailNotificationService, ITransientDependency
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendTicketCreatedConfirmationAsync(
        string toEmail,
        string requesterName,
        string ticketNumber,
        string title,
        string description,
        DateTime? dueDate,
        string? portalUrl = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        try
        {
            var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
            var viewUrl = portalUrl ?? $"{angularUrl}/portal/tickets";

            var subject = $"[Helpdesk] Xác nhận tiếp nhận sự vụ #{ticketNumber}: {title}";
            var body = BuildEmailTemplate(
                headerTitle: "XÁC NHẬN TIẾP NHẬN SỰ VỤ",
                headerColor: "#3498db",
                greeting: $"Kính chào <strong>{requesterName}</strong>,",
                messageContent: $"Yêu cầu hỗ trợ của bạn đã được ghi nhận vào hệ thống Helpdesk ITSM với mã số <strong>{ticketNumber}</strong>.",
                fields: new (string, string)[]
                {
                    ("Mã sự vụ", ticketNumber),
                    ("Tiêu đề", title),
                    ("Mô tả", string.IsNullOrWhiteSpace(description) ? "(Không có)" : description),
                    ("Hạn chót cam kết (SLA)", dueDate?.ToString("HH:mm dd/MM/yyyy") ?? "Đang tính toán"),
                    ("Trạng thái", "Mới tiếp nhận (Open)")
                },
                buttonText: "Theo dõi tiến độ sự vụ",
                buttonUrl: viewUrl
            );

            await _emailSender.SendAsync(toEmail, subject, body, isBodyHtml: true);
            _logger.LogInformation("Đã gửi email xác nhận tạo vé {TicketNumber} tới {Email}", ticketNumber, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi email xác nhận tạo vé {TicketNumber} tới {Email}", ticketNumber, toEmail);
        }
    }

    public async Task SendTicketResolvedNotificationAsync(
        string toEmail,
        string requesterName,
        string ticketNumber,
        string title,
        string? resolutionNote,
        string? csatUrl = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        try
        {
            var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
            var viewUrl = csatUrl ?? $"{angularUrl}/portal/tickets";

            var subject = $"[Helpdesk] Sự vụ #{ticketNumber} đã được giải quyết - Khảo sát độ hài lòng";
            var body = BuildEmailTemplate(
                headerTitle: "SỰ VỤ ĐÃ ĐƯỢC GIẢI QUYẾT",
                headerColor: "#2ecc71",
                greeting: $"Kính chào <strong>{requesterName}</strong>,",
                messageContent: $"Sự vụ <strong>[{ticketNumber}] {title}</strong> của bạn đã được đội ngũ kỹ thuật xử lý hoàn tất. Vui lòng kiểm tra lại dịch vụ của bạn.",
                fields: new (string, string)[]
                {
                    ("Mã sự vụ", ticketNumber),
                    ("Tiêu đề", title),
                    ("Ghi chú giải quyết", string.IsNullOrWhiteSpace(resolutionNote) ? "Sự vụ đã được khắc phục thành công." : resolutionNote),
                    ("Trạng thái", "Đã giải quyết (Resolved)")
                },
                buttonText: "Đánh giá chất lượng dịch vụ (CSAT)",
                buttonUrl: viewUrl
            );

            await _emailSender.SendAsync(toEmail, subject, body, isBodyHtml: true);
            _logger.LogInformation("Đã gửi email thông báo giải quyết vé {TicketNumber} tới {Email}", ticketNumber, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi email thông báo giải quyết vé {TicketNumber} tới {Email}", ticketNumber, toEmail);
        }
    }

    public async Task SendTicketAssignedNotificationAsync(
        string toEmail,
        string assigneeName,
        string ticketNumber,
        string title,
        string priorityName,
        DateTime? dueDate,
        string? ticketUrl = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        try
        {
            var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
            var viewUrl = ticketUrl ?? $"{angularUrl}/tickets";

            var subject = $"[Helpdesk Phân công] Bạn được giao xử lý sự vụ #{ticketNumber}";
            var body = BuildEmailTemplate(
                headerTitle: "PHÂN CÔNG XỬ LÝ SỰ VỤ MỚI",
                headerColor: "#e67e22",
                greeting: $"Xin chào kỹ thuật viên <strong>{assigneeName}</strong>,",
                messageContent: $"Bạn vừa được phân công phụ trách xử lý sự vụ <strong>[{ticketNumber}] {title}</strong>. Vui lòng tiếp nhận và phản hồi khách hàng theo đúng cam kết SLA.",
                fields: new (string, string)[]
                {
                    ("Mã sự vụ", ticketNumber),
                    ("Tiêu đề", title),
                    ("Mức độ ưu tiên", priorityName),
                    ("Hạn chót giải quyết (SLA)", dueDate?.ToString("HH:mm dd/MM/yyyy") ?? "Chưa xác định")
                },
                buttonText: "Mở chi tiết sự vụ trên Web",
                buttonUrl: viewUrl
            );

            await _emailSender.SendAsync(toEmail, subject, body, isBodyHtml: true);
            _logger.LogInformation("Đã gửi email phân công vé {TicketNumber} tới kỹ thuật viên {Email}", ticketNumber, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể gửi email phân công vé {TicketNumber} tới {Email}", ticketNumber, toEmail);
        }
    }

    private static string BuildEmailTemplate(
        string headerTitle,
        string headerColor,
        string greeting,
        string messageContent,
        (string Label, string Value)[] fields,
        string buttonText,
        string buttonUrl)
    {
        var sb = new StringBuilder();
        sb.Append($@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f9; margin: 0; padding: 20px; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.08); }}
        .header {{ background-color: {headerColor}; color: #ffffff; padding: 25px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 20px; letter-spacing: 1px; font-weight: 600; }}
        .body {{ padding: 30px 25px; }}
        .greeting {{ font-size: 16px; margin-bottom: 15px; }}
        .message {{ font-size: 15px; line-height: 1.6; color: #555; margin-bottom: 25px; }}
        .info-table {{ width: 100%; border-collapse: collapse; margin-bottom: 30px; background-color: #fcfcfc; border-radius: 6px; }}
        .info-table td {{ padding: 12px 15px; border-bottom: 1px solid #edf2f7; font-size: 14px; }}
        .info-table td.label {{ font-weight: 600; color: #4a5568; width: 35%; }}
        .info-table td.val {{ color: #2d3748; }}
        .cta-container {{ text-align: center; margin: 30px 0; }}
        .btn {{ display: inline-block; padding: 14px 28px; background-color: {headerColor}; color: #ffffff !important; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 15px; box-shadow: 0 3px 6px rgba(0,0,0,0.12); }}
        .footer {{ background-color: #f8fafc; padding: 20px; text-align: center; font-size: 12px; color: #a0aec0; border-top: 1px solid #edf2f7; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{headerTitle}</h1>
        </div>
        <div class='body'>
            <div class='greeting'>{greeting}</div>
            <div class='message'>{messageContent}</div>
            <table class='info-table'>
");

        foreach (var (label, val) in fields)
        {
            sb.Append($@"
                <tr>
                    <td class='label'>{label}:</td>
                    <td class='val'>{val}</td>
                </tr>");
        }

        sb.Append($@"
            </table>
            <div class='cta-container'>
                <a href='{buttonUrl}' class='btn' target='_blank'>{buttonText} &rarr;</a>
            </div>
        </div>
        <div class='footer'>
            Hệ thống Quản lý Hỗ trợ Kỹ thuật & Cam kết Dịch vụ (Helpdesk ITSM)<br>
            Email này được gửi tự động, vui lòng không phản hồi trực tiếp vào địa chỉ này.
        </div>
    </div>
</body>
</html>
");
        return sb.ToString();
    }
}
