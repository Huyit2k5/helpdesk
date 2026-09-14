using System;

namespace Helpdesk.Discord;

public class DiscordLinkCodeDto
{
    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
