using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Helpdesk.Settings;
using Helpdesk.Tickets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Helpdesk.Discord;

public class DiscordNotificationService : IDiscordNotificationService, ITransientDependency
{
    private readonly ISettingProvider _settingProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordNotificationService> _logger;

    public DiscordNotificationService(
        ISettingProvider settingProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DiscordNotificationService> logger)
    {
        _settingProvider = settingProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendTicketCreatedAsync(Ticket ticket, string categoryName, string priorityName, bool isCritical)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var notifyOnNewTicket = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnNewTicket);
            if (!notifyOnNewTicket) return;

            var notifyOnCriticalOnly = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnCriticalOnly);
            if (notifyOnCriticalOnly && !isCritical) return;

            var webhookUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.WebhookUrl);
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            var ticketUrl = BuildTicketUrl(ticket.Id);
            var color = isCritical ? DiscordConsts.ColorDanger : DiscordConsts.ColorInfo;
            var title = $"{(isCritical ? "🚨 [CRITICAL] " : "🎫 ")}[{ticket.TicketNumber}] {ticket.Title}";
            var description = string.IsNullOrWhiteSpace(ticket.Description)
                ? "Không có mô tả chi tiết."
                : (ticket.Description.Length > 250 ? ticket.Description.Substring(0, 247) + "..." : ticket.Description);

            var fields = new List<object>
            {
                new { name = "👤 Người yêu cầu", value = string.IsNullOrWhiteSpace(ticket.RequesterName) ? "Ẩn danh" : ticket.RequesterName, inline = true },
                new { name = "⚡ Mức ưu tiên", value = priorityName, inline = true },
                new { name = "📁 Danh mục", value = categoryName, inline = true },
                new { name = "⏱️ Hạn chót SLA", value = ticket.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa thiết lập", inline = true },
                new { name = "📌 Trạng thái", value = "Mới tiếp nhận (Open)", inline = true },
                new { name = "👨‍💻 Kỹ thuật viên", value = "Chưa phân công", inline = true }
            };

            await PostToDiscordAsync(webhookUrl, title, description, ticketUrl, color, fields);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi thông báo tạo vé mới tới Discord Webhook cho vé: {TicketNumber}", ticket.TicketNumber);
        }
    }

    public async Task SendTicketAssignedAsync(Ticket ticket, string assigneeName, string priorityName)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var notifyOnAssigned = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnAssigned);
            if (!notifyOnAssigned) return;

            var webhookUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.WebhookUrl);
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            var ticketUrl = BuildTicketUrl(ticket.Id);
            var title = $"👉 [{ticket.TicketNumber}] Đã phân công xử lý: {ticket.Title}";

            var fields = new List<object>
            {
                new { name = "👨‍💻 Kỹ thuật viên phụ trách", value = assigneeName, inline = true },
                new { name = "⚡ Mức ưu tiên", value = priorityName, inline = true },
                new { name = "⏱️ Hạn xử lý SLA", value = ticket.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A", inline = true }
            };

            await PostToDiscordAsync(webhookUrl, title, "Sự vụ đã được điều phối và chỉ định người phụ trách.", ticketUrl, DiscordConsts.ColorWarning, fields);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi thông báo phân công vé tới Discord: {TicketNumber}", ticket.TicketNumber);
        }
    }

    public async Task SendSlaBreachAsync(Ticket ticket, string breachType, DateTime dueDate, string priorityName)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var notifyOnSlaBreach = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnSlaBreach);
            if (!notifyOnSlaBreach) return;

            var webhookUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.WebhookUrl);
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            var ticketUrl = BuildTicketUrl(ticket.Id);
            var title = $"⚠️ [VI PHẠM SLA] [{ticket.TicketNumber}] {ticket.Title}";

            var fields = new List<object>
            {
                new { name = "🚨 Loại vi phạm", value = breachType, inline = true },
                new { name = "⚡ Mức ưu tiên", value = priorityName, inline = true },
                new { name = "⏰ Hạn cam kết SLA", value = dueDate.ToString("dd/MM/yyyy HH:mm"), inline = true },
                new { name = "👤 Người yêu cầu", value = string.IsNullOrWhiteSpace(ticket.RequesterName) ? "N/A" : ticket.RequesterName, inline = true }
            };

            await PostToDiscordAsync(webhookUrl, title, "Sự vụ đã vượt quá thời hạn cam kết SLA. Vui lòng ưu tiên xử lý khẩn cấp!", ticketUrl, DiscordConsts.ColorDanger, fields);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi cảnh báo vi phạm SLA tới Discord cho vé: {TicketNumber}", ticket.TicketNumber);
        }
    }

    public async Task SendTicketResolvedAsync(Ticket ticket, string resolvedByName)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var notifyOnResolved = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnResolved);
            if (!notifyOnResolved) return;

            var webhookUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.WebhookUrl);
            if (string.IsNullOrWhiteSpace(webhookUrl)) return;

            var ticketUrl = BuildTicketUrl(ticket.Id);
            var title = $"✅ [{ticket.TicketNumber}] Sự vụ đã được giải quyết: {ticket.Title}";

            var fields = new List<object>
            {
                new { name = "👨‍💻 Kỹ thuật viên xử lý", value = resolvedByName, inline = true },
                new { name = "⏱️ Thời gian giải quyết", value = ticket.ResolvedAt?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"), inline = true }
            };

            await PostToDiscordAsync(webhookUrl, title, "Sự vụ đã được đánh dấu giải quyết thành công.", ticketUrl, DiscordConsts.ColorSuccess, fields);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi thông báo giải quyết vé tới Discord: {TicketNumber}", ticket.TicketNumber);
        }
    }

    public async Task<bool> SendTestMessageAsync(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL không được để trống.", nameof(webhookUrl));
        }

        var title = "🔔 [HELPDESK] Tin nhắn kiểm tra kết nối Discord Webhook";
        var description = "Hệ thống Helpdesk đã kết nối thành công tới kênh Discord này! Mọi cảnh báo sự cố, phân công vé và thông báo SLA sẽ được đẩy về đây theo cấu hình đã chọn.";
        var fields = new List<object>
        {
            new { name = "🕒 Thời gian kiểm tra", value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), inline = true },
            new { name = "🌐 Trạng thái kết nối", value = "✅ Hoạt động tốt (Active)", inline = true }
        };

        return await PostToDiscordAsync(webhookUrl, title, description, null, DiscordConsts.ColorInfo, fields);
    }

    private async Task<bool> PostToDiscordAsync(
        string webhookUrl,
        string title,
        string description,
        string? url,
        int color,
        List<object> fields)
    {
        var botName = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.BotName);
        if (string.IsNullOrWhiteSpace(botName)) botName = DiscordConsts.DefaultBotName;

        var avatarUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.AvatarUrl);
        if (string.IsNullOrWhiteSpace(avatarUrl)) avatarUrl = DiscordConsts.DefaultAvatarUrl;

        var embed = new Dictionary<string, object>
        {
            ["title"] = title,
            ["description"] = description,
            ["color"] = color,
            ["fields"] = fields,
            ["footer"] = new
            {
                text = "Helpdesk ITSM System • Tự động gửi từ hệ thống",
                icon_url = "https://abp.io/assets/png/abp-logo.png"
            },
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        };

        if (!string.IsNullOrWhiteSpace(url))
        {
            embed["url"] = url;
        }

        var payload = new
        {
            username = botName,
            avatar_url = avatarUrl,
            embeds = new[] { embed }
        };

        var json = JsonSerializer.Serialize(payload);
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(webhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Discord Webhook trả về mã lỗi HTTP {StatusCode}: {Error}", response.StatusCode, errContent);
            return false;
        }

        return true;
    }

    private string BuildTicketUrl(Guid ticketId)
    {
        var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        return $"{angularUrl}/tickets/{ticketId}";
    }
}
