namespace Helpdesk.Discord;

public static class DiscordConsts
{
    public const int MaxWebhookUrlLength = 500;
    public const int MaxBotNameLength = 100;
    public const int MaxAvatarUrlLength = 500;

    public const string DefaultBotName = "Helpdesk Support Bot";
    public const string DefaultAvatarUrl = "https://cdn-icons-png.flaticon.com/512/4712/4712035.png";

    // Discord Embed Colors (Decimal integer)
    public const int ColorInfo = 0x3498db;     // Xanh dương: Vé mới, thông tin thường
    public const int ColorWarning = 0xf39c12;  // Vàng cam: Cảnh báo, phân công
    public const int ColorDanger = 0xe74c3c;   // Đỏ: Khẩn cấp (Critical), Vi phạm SLA
    public const int ColorSuccess = 0x2ecc71;  // Xanh lá: Giải quyết hoàn tất (Resolved)
}
