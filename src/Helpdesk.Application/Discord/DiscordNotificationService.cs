using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Helpdesk.Settings;
using Helpdesk.Tickets;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Helpdesk.Discord;

public class DiscordNotificationService : IDiscordNotificationService, ITransientDependency
{
    private readonly ISettingProvider _settingProvider;
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiscordNotificationService> _logger;

    public DiscordNotificationService(
        ISettingProvider settingProvider,
        IBackgroundJobManager backgroundJobManager,
        IHttpClientFactory httpClientFactory,
        ILogger<DiscordNotificationService> logger)
    {
        _settingProvider = settingProvider;
        _backgroundJobManager = backgroundJobManager;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendTicketCreatedAsync(Ticket ticket, string categoryName, string priorityName, bool isCritical)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var args = new DiscordNotificationArgs
            {
                Type = DiscordNotificationType.TicketCreated,
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                Description = ticket.Description,
                RequesterName = ticket.RequesterName,
                PriorityName = priorityName,
                CategoryName = categoryName,
                DueDate = ticket.DueDate,
                IsCritical = isCritical
            };

            await _backgroundJobManager.EnqueueAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi enqueue job gửi thông báo tạo vé mới tới Discord: {TicketNumber}", ticket.TicketNumber);
        }
    }

    public async Task SendTicketAssignedAsync(Ticket ticket, string assigneeName, string priorityName)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var args = new DiscordNotificationArgs
            {
                Type = DiscordNotificationType.TicketAssigned,
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                PriorityName = priorityName,
                DueDate = ticket.DueDate,
                AssigneeName = assigneeName
            };

            await _backgroundJobManager.EnqueueAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi enqueue job thông báo phân công vé tới Discord: {TicketNumber}", ticket.TicketNumber);
        }
    }

    public async Task SendSlaBreachAsync(Ticket ticket, string breachType, DateTime dueDate, string priorityName)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var args = new DiscordNotificationArgs
            {
                Type = DiscordNotificationType.SlaBreached,
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                PriorityName = priorityName,
                DueDate = dueDate,
                BreachType = breachType,
                RequesterName = ticket.RequesterName
            };

            await _backgroundJobManager.EnqueueAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi enqueue job cảnh báo vi phạm SLA tới Discord cho vé: {TicketNumber}", ticket.TicketNumber);
        }
    }

    public async Task SendTicketResolvedAsync(Ticket ticket, string resolvedByName)
    {
        try
        {
            var isEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            if (!isEnabled) return;

            var args = new DiscordNotificationArgs
            {
                Type = DiscordNotificationType.TicketResolved,
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                Title = ticket.Title,
                ResolvedByName = resolvedByName,
                ResolvedAt = ticket.ResolvedAt ?? DateTime.Now
            };

            await _backgroundJobManager.EnqueueAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi enqueue job thông báo giải quyết vé tới Discord: {TicketNumber}", ticket.TicketNumber);
        }
    }

    /// <summary>
    /// Gửi tin nhắn kiểm tra trực tiếp (đồng bộ) để người quản trị nhận kết quả ngay tức thì trên giao diện Web.
    /// </summary>
    public async Task<bool> SendTestMessageAsync(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL không được để trống.", nameof(webhookUrl));
        }

        var title = "🔔 [HELPDESK] Tin nhắn kiểm tra kết nối Discord Webhook";
        var description = "Hệ thống Helpdesk đã kết nối thành công tới kênh Discord này! Mọi cảnh báo sự cố, phân công vé và thông báo SLA sẽ được đẩy về đây theo cơ chế Background Job bất đồng bộ.";
        var fields = new List<object>
        {
            new { name = "🕒 Thời gian kiểm tra", value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), inline = true },
            new { name = "⚡ Chế độ gửi", value = "🚀 ABP Background Job (Asynchronous)", inline = true },
            new { name = "🌐 Trạng thái kết nối", value = "✅ Hoạt động tốt (Active)", inline = true }
        };

        var botName = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.BotName);
        if (string.IsNullOrWhiteSpace(botName)) botName = DiscordConsts.DefaultBotName;

        var avatarUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.AvatarUrl);
        if (string.IsNullOrWhiteSpace(avatarUrl)) avatarUrl = DiscordConsts.DefaultAvatarUrl;

        var embed = new Dictionary<string, object>
        {
            ["title"] = title,
            ["description"] = description,
            ["color"] = DiscordConsts.ColorInfo,
            ["fields"] = fields,
            ["footer"] = new
            {
                text = "Helpdesk ITSM System • Tự động gửi từ hệ thống",
                icon_url = "https://abp.io/assets/png/abp-logo.png"
            },
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        };

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
}
