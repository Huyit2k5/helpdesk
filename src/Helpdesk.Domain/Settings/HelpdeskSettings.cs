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

    public static class Ai
    {
        private const string AiPrefix = Prefix + ".Ai";

        public const string IsEnabled = AiPrefix + ".IsEnabled";
        public const string Provider = AiPrefix + ".Provider"; // "BuiltInOffline", "GoogleGemini", "OpenAI", "LocalOllama"
        public const string ApiKey = AiPrefix + ".ApiKey";
        public const string ModelName = AiPrefix + ".ModelName"; // "gemini-1.5-flash", "gpt-4o-mini", etc.
        public const string BaseUrl = AiPrefix + ".BaseUrl"; // for Ollama e.g. http://localhost:11434
        public const string Temperature = AiPrefix + ".Temperature"; // "0.3"
        public const string EnableAutoSentiment = AiPrefix + ".EnableAutoSentiment"; // "true"
        public const string CustomSystemPrompt = AiPrefix + ".CustomSystemPrompt";
    }
}
