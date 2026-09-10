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
            new SettingDefinition(HelpdeskSettings.Discord.AvatarUrl, defaultValue: DiscordConsts.DefaultAvatarUrl, isVisibleToClients: true)
        );
    }
}
