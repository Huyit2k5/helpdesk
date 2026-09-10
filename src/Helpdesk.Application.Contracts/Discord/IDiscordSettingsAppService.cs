using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Helpdesk.Discord;

public interface IDiscordSettingsAppService : IApplicationService
{
    Task<DiscordSettingsDto> GetAsync();

    Task UpdateAsync(UpdateDiscordSettingsDto input);

    Task<TestDiscordResultDto> SendTestNotificationAsync(SendTestDiscordInput input);
}
