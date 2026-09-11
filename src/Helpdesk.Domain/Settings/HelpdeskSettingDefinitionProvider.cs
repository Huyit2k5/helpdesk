using Helpdesk.Discord;
using Volo.Abp.Settings;

namespace Helpdesk.Settings;

public class HelpdeskSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        context.Add(
            new SettingDefinition(HelpdeskSettings.Discord.WebhookUrl, defaultValue: string.Empty, isVisibleToClients: false),
            new SettingDefinition(HelpdeskSettings.Discord.IsEnabled, defaultValue: "false", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.NotifyOnNewTicket, defaultValue: "true", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.NotifyOnCriticalOnly, defaultValue: "false", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.NotifyOnAssigned, defaultValue: "true", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.NotifyOnSlaBreach, defaultValue: "true", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.NotifyOnResolved, defaultValue: "true", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.BotName, defaultValue: DiscordConsts.DefaultBotName, isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.AvatarUrl, defaultValue: DiscordConsts.DefaultAvatarUrl, isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Discord.BotToken, defaultValue: string.Empty, isVisibleToClients: false, isEncrypted: true),
            new SettingDefinition(HelpdeskSettings.Discord.ChannelId, defaultValue: string.Empty, isVisibleToClients: true),

            // AI Copilot Settings
            new SettingDefinition(HelpdeskSettings.Ai.IsEnabled, defaultValue: "true", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Ai.Provider, defaultValue: "BuiltInOffline", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Ai.ApiKey, defaultValue: string.Empty, isVisibleToClients: false, isEncrypted: true),
            new SettingDefinition(HelpdeskSettings.Ai.ModelName, defaultValue: "gemini-1.5-flash", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Ai.BaseUrl, defaultValue: string.Empty, isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Ai.Temperature, defaultValue: "0.3", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Ai.EnableAutoSentiment, defaultValue: "true", isVisibleToClients: true),
            new SettingDefinition(HelpdeskSettings.Ai.CustomSystemPrompt, defaultValue: string.Empty, isVisibleToClients: true)
        );
    }
}
