using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Helpdesk.Discord;

/// <summary>
/// Tự phục vụ: bất kỳ user đã đăng nhập nào cũng sinh được mã liên kết Discord cho CHÍNH tài khoản
/// mình (không cần permission riêng). Mã được dùng bởi <c>DiscordBotService.LinkUserWithCodeAsync</c>
/// khi người dùng gõ <c>/link-helpdesk &lt;mã&gt;</c> trên Discord - thay thế cho việc bot tin theo
/// username thô do người dùng gõ vào (lỗ hổng cho phép giả mạo danh tính bất kỳ ai).
/// </summary>
[Authorize]
public class DiscordAccountLinkAppService : ApplicationService, IDiscordAccountLinkAppService
{
    private const int CodeLength = 8;
    private const int ExpirationMinutes = 10;

    // Bỏ các ký tự dễ nhầm lẫn khi gõ tay: 0/O, 1/I.
    private const string CodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public DiscordAccountLinkAppService(IRepository<IdentityUser, Guid> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<DiscordLinkCodeDto> GenerateLinkCodeAsync()
    {
        if (!CurrentUser.Id.HasValue)
        {
            throw new UserFriendlyException("Không xác định được người dùng hiện tại.");
        }

        var user = await _userRepository.GetAsync(CurrentUser.Id.Value);

        var code = GenerateRandomCode();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(ExpirationMinutes);

        user.SetProperty("DiscordLinkCode", code);
        user.SetProperty("DiscordLinkCodeExpiresAtUtc", expiresAtUtc.ToString("O"));
        await _userRepository.UpdateAsync(user, autoSave: true);

        return new DiscordLinkCodeDto
        {
            Code = code,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private static string GenerateRandomCode()
    {
        Span<char> buffer = stackalloc char[CodeLength];
        for (var i = 0; i < CodeLength; i++)
        {
            var index = RandomNumberGenerator.GetInt32(CodeAlphabet.Length);
            buffer[i] = CodeAlphabet[index];
        }

        return new string(buffer);
    }
}
