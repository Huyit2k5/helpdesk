using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Helpdesk.Discord;

/// <summary>
/// Cho phép người dùng đã đăng nhập tự sinh mã liên kết Discord một lần (10 phút), thay vì
/// để bot Discord tin theo username thô do người dùng gõ vào (dễ bị giả mạo danh tính).
/// </summary>
public interface IDiscordAccountLinkAppService : IApplicationService
{
    Task<DiscordLinkCodeDto> GenerateLinkCodeAsync();
}
