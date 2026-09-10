using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Helpdesk.Settings;
using Helpdesk.Tickets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Helpdesk.Discord;

public class DiscordNotificationJob : AsyncBackgroundJob<DiscordNotificationArgs>, ITransientDependency
{
    private readonly ISettingProvider _settingProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IDiscordBotService _discordBotService;
    private readonly ILogger<DiscordNotificationJob> _logger;

    public DiscordNotificationJob(
        ISettingProvider settingProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IDiscordBotService discordBotService,
        ILogger<DiscordNotificationJob> logger)
    {
        _settingProvider = settingProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _discordBotService = discordBotService;
        _logger = logger;
    }

    public override async Task ExecuteAsync(DiscordNotificationArgs args)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var webhookUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.WebhookUrl);
            var channelIdStr = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.ChannelId);
            ulong? channelId = null;
            if (!string.IsNullOrWhiteSpace(channelIdStr) && ulong.TryParse(channelIdStr.Trim(), out var parsedChannelId))
            {
                channelId = parsedChannelId;
            }

            if (string.IsNullOrWhiteSpace(webhookUrl) && !channelId.HasValue) return;

            switch (args.Type)
            {
                case DiscordNotificationType.TicketCreated:
                    await HandleTicketCreatedAsync(args, webhookUrl, channelId);
                    break;
                case DiscordNotificationType.TicketAssigned:
                    await HandleTicketAssignedAsync(args, webhookUrl, channelId);
                    break;
                case DiscordNotificationType.SlaBreached:
                    await HandleSlaBreachedAsync(args, webhookUrl, channelId);
                    break;
                case DiscordNotificationType.TicketResolved:
                    await HandleTicketResolvedAsync(args, webhookUrl, channelId);
                    break;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogWarning(httpEx, "Sự cố kết nối khi gửi Discord Webhook ngầm cho vé: {TicketNumber}. Job sẽ được thử lại tự động.", args.TicketNumber);
            throw; // Ném ngoại lệ để ABP Background Jobs kích hoạt cơ chế tự động thử lại (Retry)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi thực thi DiscordNotificationJob cho vé: {TicketNumber}", args.TicketNumber);
            throw;
        }
    }

    private async Task HandleTicketCreatedAsync(DiscordNotificationArgs args, string? webhookUrl, ulong? channelId)
    {
        var notifyOnNewTicket = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnNewTicket);
        if (!notifyOnNewTicket) return;

        var notifyOnCriticalOnly = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnCriticalOnly);
        if (notifyOnCriticalOnly && !args.IsCritical) return;

        // Ưu tiên gửi qua Discord Bot kèm nút bấm tương tác [🎯 Nhận vé này]
        if (channelId.HasValue)
        {
            var sentViaBot = await _discordBotService.SendTicketWithButtonsAsync(
                channelId.Value,
                args.TicketId,
                args.TicketNumber,
                args.Title,
                args.Description,
                args.RequesterName,
                args.DueDate,
                args.CategoryName,
                args.PriorityName,
                args.IsCritical
            );

            if (sentViaBot)
            {
                return; // Đã gửi thành công qua Bot kèm nút bấm tương tác!
            }
        }

        // Nếu Bot chưa cấu hình hoặc gửi không thành công, fallback về gửi qua Webhook
        if (string.IsNullOrWhiteSpace(webhookUrl)) return;

        var ticketUrl = BuildTicketUrl(args.TicketId);
        var color = args.IsCritical ? DiscordConsts.ColorDanger : DiscordConsts.ColorInfo;
        var title = $"{(args.IsCritical ? "🚨 [CRITICAL] " : "🎫 ")}[{args.TicketNumber}] {args.Title}";
        var description = string.IsNullOrWhiteSpace(args.Description)
            ? "Không có mô tả chi tiết."
            : (args.Description.Length > 250 ? args.Description.Substring(0, 247) + "..." : args.Description);

        var fields = new List<object>
        {
            new { name = "👤 Người yêu cầu", value = string.IsNullOrWhiteSpace(args.RequesterName) ? "Ẩn danh" : args.RequesterName, inline = true },
            new { name = "⚡ Mức ưu tiên", value = args.PriorityName, inline = true },
            new { name = "📁 Danh mục", value = args.CategoryName, inline = true },
            new { name = "⏱️ Hạn chót SLA", value = args.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa thiết lập", inline = true },
            new { name = "📌 Trạng thái", value = "Mới tiếp nhận (Open)", inline = true },
            new { name = "👨‍💻 Kỹ thuật viên", value = "Chưa phân công", inline = true }
        };

        await PostToDiscordAsync(webhookUrl, title, description, ticketUrl, color, fields);
    }

    private async Task HandleTicketAssignedAsync(DiscordNotificationArgs args, string? webhookUrl, ulong? channelId)
    {
        var notifyOnAssigned = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnAssigned);
        if (!notifyOnAssigned) return;

        var ticketUrl = BuildTicketUrl(args.TicketId);
        var title = $"👉 [{args.TicketNumber}] Đã phân công xử lý: {args.Title}";
        var description = "Sự vụ đã được điều phối và chỉ định người phụ trách.";

        var fields = new List<object>
        {
            new { name = "👨‍💻 Kỹ thuật viên phụ trách", value = args.AssigneeName ?? "Kỹ thuật viên", inline = true },
            new { name = "⚡ Mức ưu tiên", value = args.PriorityName, inline = true },
            new { name = "⏱️ Hạn xử lý SLA", value = args.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A", inline = true }
        };

        if (channelId.HasValue)
        {
            var sent = await _discordBotService.SendEmbedMessageAsync(channelId.Value, title, description, ticketUrl, DiscordConsts.ColorWarning, fields);
            if (sent) return;
        }

        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            await PostToDiscordAsync(webhookUrl, title, description, ticketUrl, DiscordConsts.ColorWarning, fields);
        }
    }

    private async Task HandleSlaBreachedAsync(DiscordNotificationArgs args, string? webhookUrl, ulong? channelId)
    {
        var notifyOnSlaBreach = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnSlaBreach);
        if (!notifyOnSlaBreach) return;

        var ticketUrl = BuildTicketUrl(args.TicketId);
        var title = $"⚠️ [VI PHẠM SLA] [{args.TicketNumber}] {args.Title}";
        var description = "Sự vụ đã vượt quá thời hạn cam kết SLA. Vui lòng ưu tiên xử lý khẩn cấp!";

        var fields = new List<object>
        {
            new { name = "🚨 Loại vi phạm", value = args.BreachType ?? "Vi phạm cam kết", inline = true },
            new { name = "⚡ Mức ưu tiên", value = args.PriorityName, inline = true },
            new { name = "⏰ Hạn cam kết SLA", value = args.DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "N/A", inline = true },
            new { name = "👤 Người yêu cầu", value = string.IsNullOrWhiteSpace(args.RequesterName) ? "N/A" : args.RequesterName, inline = true }
        };

        if (channelId.HasValue)
        {
            var sent = await _discordBotService.SendEmbedMessageAsync(channelId.Value, title, description, ticketUrl, DiscordConsts.ColorDanger, fields);
            if (sent) return;
        }

        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            await PostToDiscordAsync(webhookUrl, title, description, ticketUrl, DiscordConsts.ColorDanger, fields);
        }
    }

    private async Task HandleTicketResolvedAsync(DiscordNotificationArgs args, string? webhookUrl, ulong? channelId)
    {
        var notifyOnResolved = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnResolved);
        if (!notifyOnResolved) return;

        var ticketUrl = BuildTicketUrl(args.TicketId);
        var title = $"✅ [{args.TicketNumber}] Sự vụ đã được giải quyết: {args.Title}";
        var description = "Sự vụ đã được đánh dấu giải quyết thành công.";

        var fields = new List<object>
        {
            new { name = "👨‍💻 Kỹ thuật viên xử lý", value = args.ResolvedByName ?? "Kỹ thuật viên", inline = true },
            new { name = "⏱️ Thời gian giải quyết", value = args.ResolvedAt?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"), inline = true }
        };

        if (channelId.HasValue)
        {
            var sent = await _discordBotService.SendEmbedMessageAsync(channelId.Value, title, description, ticketUrl, DiscordConsts.ColorSuccess, fields);
            if (sent) return;
        }

        if (!string.IsNullOrWhiteSpace(webhookUrl))
        {
            await PostToDiscordAsync(webhookUrl, title, description, ticketUrl, DiscordConsts.ColorSuccess, fields);
        }
    }

    private async Task PostToDiscordAsync(
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
            _logger.LogWarning("Discord Webhook trả về HTTP {StatusCode}: {Error}", response.StatusCode, errContent);

            // Nếu gặp lỗi rate limit (429) hoặc server Discord lỗi (>= 500), ném ngoại lệ để Background Job kích hoạt Retry
            if (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500)
            {
                throw new HttpRequestException($"Discord API phản hồi lỗi tạm thời {response.StatusCode}: {errContent}");
            }
        }
    }

    private string BuildTicketUrl(Guid ticketId)
    {
        var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        return $"{angularUrl}/tickets/{ticketId}";
    }
}
