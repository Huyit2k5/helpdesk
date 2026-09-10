namespace Helpdesk.Settings;

public static class HelpdeskSettings
{
    private const string Prefix = "Helpdesk";

    public static class Discord
    {
        private const string DiscordPrefix = Prefix + ".Discord";

        public const string WebhookUrl = DiscordPrefix + ".WebhookUrl";
        public const string IsEnabled = DiscordPrefix + ".IsEnabled";
        public const string NotifyOnNewTicket = DiscordPrefix + ".NotifyOnNewTicket";
        public const string NotifyOnCriticalOnly = DiscordPrefix + ".NotifyOnCriticalOnly";
        public const string NotifyOnAssigned = DiscordPrefix + ".NotifyOnAssigned";
        public const string NotifyOnSlaBreach = DiscordPrefix + ".NotifyOnSlaBreach";
        public const string NotifyOnResolved = DiscordPrefix + ".NotifyOnResolved";
        public const string BotName = DiscordPrefix + ".BotName";
        public const string AvatarUrl = DiscordPrefix + ".AvatarUrl";
        public const string BotToken = DiscordPrefix + ".BotToken";
        public const string ChannelId = DiscordPrefix + ".ChannelId";
    }
}
