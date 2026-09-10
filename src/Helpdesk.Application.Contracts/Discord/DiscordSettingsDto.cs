using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Discord;

public class DiscordSettingsDto
{
    public string WebhookUrl { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool NotifyOnNewTicket { get; set; }

    public bool NotifyOnCriticalOnly { get; set; }

    public bool NotifyOnAssigned { get; set; }

    public bool NotifyOnSlaBreach { get; set; }

    public bool NotifyOnResolved { get; set; }

    public string BotName { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = string.Empty;
}

public class UpdateDiscordSettingsDto
{
    [MaxLength(DiscordConsts.MaxWebhookUrlLength)]
    public string WebhookUrl { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public bool NotifyOnNewTicket { get; set; }

    public bool NotifyOnCriticalOnly { get; set; }

    public bool NotifyOnAssigned { get; set; }

    public bool NotifyOnSlaBreach { get; set; }

    public bool NotifyOnResolved { get; set; }

    [MaxLength(DiscordConsts.MaxBotNameLength)]
    public string BotName { get; set; } = string.Empty;

    [MaxLength(DiscordConsts.MaxAvatarUrlLength)]
    public string AvatarUrl { get; set; } = string.Empty;
}

public class SendTestDiscordInput
{
    [MaxLength(DiscordConsts.MaxWebhookUrlLength)]
    public string? WebhookUrl { get; set; }
}

public class TestDiscordResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
