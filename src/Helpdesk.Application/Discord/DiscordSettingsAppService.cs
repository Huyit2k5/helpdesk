using System;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Helpdesk.Settings;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Helpdesk.Discord;

[Authorize(HelpdeskPermissions.DiscordSettings.Manage)]
public class DiscordSettingsAppService : ApplicationService, IDiscordSettingsAppService
{
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;
    private readonly IDiscordNotificationService _discordNotificationService;
    private readonly IDiscordBotService _discordBotService;

    public DiscordSettingsAppService(
        ISettingProvider settingProvider,
        ISettingManager settingManager,
        IDiscordNotificationService discordNotificationService,
        IDiscordBotService discordBotService)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
        _discordNotificationService = discordNotificationService;
        _discordBotService = discordBotService;
    }

    public async Task<DiscordSettingsDto> GetAsync()
    {
        return new DiscordSettingsDto
        {
            WebhookUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.WebhookUrl) ?? string.Empty,
            IsEnabled = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled),
            NotifyOnNewTicket = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnNewTicket),
            NotifyOnCriticalOnly = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnCriticalOnly),
            NotifyOnAssigned = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnAssigned),
            NotifyOnSlaBreach = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnSlaBreach),
            NotifyOnResolved = await _settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.NotifyOnResolved),
            BotName = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.BotName) ?? DiscordConsts.DefaultBotName,
            AvatarUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.AvatarUrl) ?? DiscordConsts.DefaultAvatarUrl,
            BotToken = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.BotToken) ?? string.Empty,
            ChannelId = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.ChannelId) ?? string.Empty
        };
    }

    public async Task UpdateAsync(UpdateDiscordSettingsDto input)
    {
        await SetSettingAsync(HelpdeskSettings.Discord.WebhookUrl, input.WebhookUrl?.Trim() ?? string.Empty);
        await SetSettingAsync(HelpdeskSettings.Discord.IsEnabled, input.IsEnabled.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Discord.NotifyOnNewTicket, input.NotifyOnNewTicket.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Discord.NotifyOnCriticalOnly, input.NotifyOnCriticalOnly.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Discord.NotifyOnAssigned, input.NotifyOnAssigned.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Discord.NotifyOnSlaBreach, input.NotifyOnSlaBreach.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Discord.NotifyOnResolved, input.NotifyOnResolved.ToString().ToLowerInvariant());
        await SetSettingAsync(HelpdeskSettings.Discord.BotName, string.IsNullOrWhiteSpace(input.BotName) ? DiscordConsts.DefaultBotName : input.BotName.Trim());
        await SetSettingAsync(HelpdeskSettings.Discord.AvatarUrl, string.IsNullOrWhiteSpace(input.AvatarUrl) ? DiscordConsts.DefaultAvatarUrl : input.AvatarUrl.Trim());

        if (input.BotToken != null)
        {
            await SetSettingAsync(HelpdeskSettings.Discord.BotToken, input.BotToken.Trim());
        }
        if (input.ChannelId != null)
        {
            await SetSettingAsync(HelpdeskSettings.Discord.ChannelId, input.ChannelId.Trim());
        }

        try
        {
            await _discordBotService.StopAsync();
            await _discordBotService.StartAsync();
        }
        catch { }
    }

    public async Task<TestDiscordResultDto> SendTestNotificationAsync(SendTestDiscordInput input)
    {
        var targetUrl = input.WebhookUrl?.Trim();
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            targetUrl = await _settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.WebhookUrl);
        }

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return new TestDiscordResultDto
            {
                Success = false,
                Message = "Webhook URL đang để trống. Vui lòng nhập URL Discord Webhook để thử nghiệm."
            };
        }

        if (!targetUrl.StartsWith("https://discord.com/api/webhooks/") &&
            !targetUrl.StartsWith("https://discordapp.com/api/webhooks/"))
        {
            return new TestDiscordResultDto
            {
                Success = false,
                Message = "URL không đúng định dạng Discord Webhook (phải bắt đầu bằng https://discord.com/api/webhooks/...)."
            };
        }

        try
        {
            var ok = await _discordNotificationService.SendTestMessageAsync(targetUrl);
            return new TestDiscordResultDto
            {
                Success = ok,
                Message = ok
                    ? "Đã gửi tin nhắn thử nghiệm thành công tới kênh Discord! Vui lòng kiểm tra kênh chat của bạn."
                    : "Discord trả về mã lỗi. Vui lòng kiểm tra lại Webhook URL hoặc quyền của Webhook trong kênh."
            };
        }
        catch (Exception ex)
        {
            return new TestDiscordResultDto
            {
                Success = false,
                Message = $"Không thể gửi tới Discord: {ex.Message}"
            };
        }
    }

    private async Task SetSettingAsync(string name, string value)
    {
        if (CurrentTenant.Id.HasValue)
        {
            await _settingManager.SetForTenantAsync(CurrentTenant.Id.Value, name, value);
        }
        else
        {
            await _settingManager.SetGlobalAsync(name, value);
        }
    }
}
