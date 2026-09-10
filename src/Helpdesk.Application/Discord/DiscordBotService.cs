using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Helpdesk.Categories;
using Helpdesk.Notifications;
using Helpdesk.Priorities;
using Helpdesk.Settings;
using Helpdesk.Tickets;
using Helpdesk.TicketStatuses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.Uow;

namespace Helpdesk.Discord;

public class DiscordBotService : IDiscordBotService, ISingletonDependency, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordBotService> _logger;
    private DiscordSocketClient? _client;
    private bool _isStarting;

    public bool IsConnected => _client != null && _client.ConnectionState == ConnectionState.Connected;

    public DiscordBotService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<DiscordBotService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        if (_isStarting || (_client != null && _client.ConnectionState == ConnectionState.Connected))
        {
            return;
        }

        try
        {
            _isStarting = true;
            using var scope = _serviceScopeFactory.CreateScope();
            var settingProvider = scope.ServiceProvider.GetRequiredService<ISettingProvider>();

            var isEnabled = await settingProvider.GetAsync<bool>(HelpdeskSettings.Discord.IsEnabled);
            var token = await settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.BotToken);

            if (!isEnabled || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogInformation("Discord Bot chưa được bật hoặc chưa nhập Bot Token.");
                return;
            }

            if (_client == null)
            {
                var config = new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent,
                    LogLevel = LogSeverity.Info
                };

                _client = new DiscordSocketClient(config);
                _client.Log += OnLogAsync;
                _client.Ready += OnReadyAsync;
                _client.ButtonExecuted += OnButtonExecutedAsync;
                _client.SlashCommandExecuted += OnSlashCommandExecutedAsync;
                _client.ModalSubmitted += OnModalSubmittedAsync;
                _client.MessageReceived += OnMessageReceivedAsync;
            }

            await _client.LoginAsync(TokenType.Bot, token.Trim());
            await _client.StartAsync();
            _logger.LogInformation("Discord Bot client đã khởi động kết nối Gateway...");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi khởi động Discord Bot.");
        }
        finally
        {
            _isStarting = false;
        }
    }

    public async Task StopAsync()
    {
        if (_client != null)
        {
            try
            {
                await _client.StopAsync();
                await _client.LogoutAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi dừng Discord Bot.");
            }
        }
    }

    public async Task<bool> SendTicketWithButtonsAsync(
        ulong channelId,
        Guid ticketId,
        string ticketNumber,
        string title,
        string? description,
        string requesterName,
        DateTime? dueDate,
        string categoryName,
        string priorityName,
        bool isCritical)
    {
        if (_client == null || _client.ConnectionState != ConnectionState.Connected)
        {
            return false;
        }

        try
        {
            var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
            if (channel == null)
            {
                _logger.LogWarning("Không tìm thấy Discord Text Channel với ID: {ChannelId}", channelId);
                return false;
            }

            var ticketUrl = BuildTicketUrl(ticketId);
            var color = isCritical ? new Color(0xe7, 0x4c, 0x3c) : new Color(0x34, 0x98, 0xdb);
            var embedTitle = $"{(isCritical ? "🚨 [CRITICAL] " : "🎫 ")}[{ticketNumber}] {title}";
            var embedDesc = string.IsNullOrWhiteSpace(description)
                ? "Không có mô tả chi tiết."
                : (description.Length > 250 ? description.Substring(0, 247) + "..." : description);

            var embed = new EmbedBuilder()
                .WithTitle(embedTitle)
                .WithDescription(embedDesc)
                .WithUrl(ticketUrl)
                .WithColor(color)
                .AddField("👤 Người yêu cầu", string.IsNullOrWhiteSpace(requesterName) ? "Ẩn danh" : requesterName, inline: true)
                .AddField("⚡ Mức ưu tiên", priorityName, inline: true)
                .AddField("📁 Danh mục", categoryName, inline: true)
                .AddField("⏱️ Hạn chót SLA", dueDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa thiết lập", inline: true)
                .AddField("📌 Trạng thái", "Mới tiếp nhận (Open)", inline: true)
                .AddField("👨‍💻 Kỹ thuật viên", "Chưa phân công", inline: true)
                .WithFooter("Helpdesk ITSM System • Nút nhận vé trực tiếp", "https://abp.io/assets/png/abp-logo.png")
                .WithCurrentTimestamp()
                .Build();

            var components = new ComponentBuilder()
                .WithButton("🎯 Nhận vé này", $"claim_ticket_{ticketId}", ButtonStyle.Primary)
                .WithButton("👁️ Xem trên Web", style: ButtonStyle.Link, url: ticketUrl)
                .Build();

            await channel.SendMessageAsync(embed: embed, components: components);
            _logger.LogInformation("Đã gửi thông báo vé [{TicketNumber}] kèm nút bấm vào kênh Discord {ChannelId}", ticketNumber, channelId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi tin nhắn kèm nút tới Discord cho vé {TicketNumber}", ticketNumber);
            return false;
        }
    }

    public async Task<bool> SendEmbedMessageAsync(
        ulong channelId,
        string title,
        string description,
        string? url,
        int color,
        List<object> fields)
    {
        if (_client == null || _client.ConnectionState != ConnectionState.Connected)
        {
            return false;
        }

        try
        {
            var channel = await _client.GetChannelAsync(channelId) as IMessageChannel;
            if (channel == null) return false;

            var embedBuilder = new EmbedBuilder()
                .WithTitle(title)
                .WithDescription(description)
                .WithColor(new Color((uint)color))
                .WithFooter("Helpdesk ITSM System • Tự động gửi từ hệ thống", "https://abp.io/assets/png/abp-logo.png")
                .WithCurrentTimestamp();

            if (!string.IsNullOrWhiteSpace(url))
            {
                embedBuilder.WithUrl(url);
            }

            foreach (var f in fields)
            {
                var type = f.GetType();
                var nameProp = type.GetProperty("name")?.GetValue(f)?.ToString() ?? "";
                var valProp = type.GetProperty("value")?.GetValue(f)?.ToString() ?? "";
                var inlineProp = type.GetProperty("inline")?.GetValue(f) as bool? ?? true;
                embedBuilder.AddField(nameProp, valProp, inlineProp);
            }

            await channel.SendMessageAsync(embed: embedBuilder.Build());
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi embed qua Bot tới channel {ChannelId}", channelId);
            return false;
        }
    }

    private Task OnLogAsync(LogMessage msg)
    {
        _logger.LogInformation("Discord.Net [{Severity}]: {Message}", msg.Severity, msg.Message);
        return Task.CompletedTask;
    }

    private Task OnReadyAsync()
    {
        _logger.LogInformation("Discord Bot đã kết nối thành công với tên: {CurrentUser}", _client?.CurrentUser.Username);

        _ = Task.Run(async () =>
        {
            try
            {
                // 1. Đăng ký Slash Command: /link-helpdesk <username>
                var linkCommand = new SlashCommandBuilder()
                    .WithName("link-helpdesk")
                    .WithDescription("Liên kết tài khoản Discord của bạn với tài khoản kỹ thuật viên Helpdesk")
                    .AddOption("username", ApplicationCommandOptionType.String, "Tên đăng nhập trong Helpdesk (Ví dụ: staff1, admin)", isRequired: true);

                // 2. Đăng ký Slash Command: /my-tickets
                var myTicketsCommand = new SlashCommandBuilder()
                    .WithName("my-tickets")
                    .WithDescription("Xem danh sách tất cả các sự vụ đang được phân công cho bạn");

                var linkBuilt = linkCommand.Build();
                var myTicketsBuilt = myTicketsCommand.Build();

                // Đăng ký trực tiếp cho từng Guild (Server) để hiển thị NGAY LẬP TỨC trên Discord
                if (_client != null)
                {
                    foreach (var guild in _client.Guilds)
                    {
                        try
                        {
                            await guild.CreateApplicationCommandAsync(linkBuilt);
                            await guild.CreateApplicationCommandAsync(myTicketsBuilt);
                            _logger.LogInformation("Đã đăng ký Guild Slash Command thành công cho server: {GuildName} ({GuildId})", guild.Name, guild.Id);
                        }
                        catch (Exception gEx)
                        {
                            _logger.LogWarning(gEx, "Không thể đăng ký Guild Slash Command cho server {GuildName}: {Message}", guild.Name, gEx.Message);
                        }
                    }

                    // Đồng thời đăng ký Global
                    await _client.CreateGlobalApplicationCommandAsync(linkBuilt);
                    await _client.CreateGlobalApplicationCommandAsync(myTicketsBuilt);
                }

                _logger.LogInformation("Đã hoàn tất đăng ký Slash Command (/link-helpdesk, /my-tickets) cấp Guild và Global.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi đăng ký Slash Command cho Discord Bot.");
            }
        });

        return Task.CompletedTask;
    }

    private async Task OnButtonExecutedAsync(SocketMessageComponent component)
    {
        try
        {
            // 1. Xử lý nút [🏁 Hoàn thành vé] -> Hiển thị Modal nhập ghi chú
            if (component.Data.CustomId.StartsWith("resolve_ticket_"))
            {
                var ticketIdStr = component.Data.CustomId.Replace("resolve_ticket_", "");
                var modal = new ModalBuilder()
                    .WithTitle("Hoàn thành sự vụ")
                    .WithCustomId($"resolve_modal_{ticketIdStr}")
                    .AddTextInput(
                        "Ghi chú / Cách khắc phục sự cố",
                        "resolution_note",
                        TextInputStyle.Paragraph,
                        placeholder: "Nhập tóm tắt cách bạn đã xử lý sự vụ thành công (tùy chọn)...",
                        required: false,
                        maxLength: 1000
                    )
                    .Build();

                await component.RespondWithModalAsync(modal);
                return;
            }

            // 2. Xử lý nút [🎯 Nhận vé này]
            if (!component.Data.CustomId.StartsWith("claim_ticket_"))
            {
                return;
            }

            await component.DeferAsync(ephemeral: true);

            var ticketIdToClaim = component.Data.CustomId.Replace("claim_ticket_", "");
            if (!Guid.TryParse(ticketIdToClaim, out var ticketId))
            {
                await component.FollowupAsync("❌ ID sự vụ không hợp lệ.", ephemeral: true);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ticket, Guid>>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();
            var statusRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketStatus, Guid>>();
            var ticketManager = scope.ServiceProvider.GetRequiredService<TicketManager>();
            var activityRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketActivity, Guid>>();
            var notificationManager = scope.ServiceProvider.GetRequiredService<NotificationManager>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using var uow = uowManager.Begin();

            var allUsers = await userRepo.GetListAsync();
            var discordUserIdStr = component.User.Id.ToString();

            // Tìm nhân viên theo DiscordUserId hoặc Username
            var user = allUsers.FirstOrDefault(u => u.GetProperty<string>("DiscordUserId") == discordUserIdStr)
                       ?? allUsers.FirstOrDefault(u => string.Equals(u.UserName, component.User.Username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                await component.FollowupAsync(
                    $"⚠️ Tài khoản Discord của bạn (**@{component.User.Username}**) chưa được liên kết với nhân viên nào trong Helpdesk.\n" +
                    $"👉 Vui lòng gõ lệnh: `/link-helpdesk <tên_đăng_nhập>` (Ví dụ: `/link-helpdesk staff1`) để liên kết và nhận vé!",
                    ephemeral: true);
                return;
            }

            var ticket = await ticketRepo.FindAsync(ticketId);
            if (ticket == null)
            {
                await component.FollowupAsync("❌ Sự vụ này không tồn tại hoặc đã bị xóa khỏi hệ thống.", ephemeral: true);
                return;
            }

            if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != Guid.Empty)
            {
                var currentAssignee = allUsers.FirstOrDefault(u => u.Id == ticket.AssigneeId.Value);
                await component.FollowupAsync(
                    $"⚠️ Vé **[{ticket.TicketNumber}]** đã được tiếp nhận trước đó bởi kỹ thuật viên **{currentAssignee?.UserName ?? "khác"}**!",
                    ephemeral: true);
                return;
            }

            // Tiến hành phân công vé
            await ticketManager.AssignAsync(ticket, user.Id, user.UserName, ticket.DepartmentId, null);

            // Chuyển trạng thái vé sang Đang xử lý (In Progress)
            var allStatuses = await statusRepo.GetListAsync();
            var inProgressStatus = allStatuses.FirstOrDefault(s => s.StatusGroup == StatusGroup.InProgress)
                                 ?? allStatuses.FirstOrDefault(s => s.Code == "IN_PROGRESS" || s.Name.Contains("Progress", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Đang xử lý", StringComparison.OrdinalIgnoreCase));

            if (inProgressStatus != null && ticket.StatusId != inProgressStatus.Id)
            {
                var oldStatus = allStatuses.FirstOrDefault(s => s.Id == ticket.StatusId);
                if (oldStatus != null)
                {
                    await ticketManager.ChangeStatusAsync(ticket, oldStatus, inProgressStatus);
                }
                else
                {
                    ticket.StatusId = inProgressStatus.Id;
                }
            }

            await ticketRepo.UpdateAsync(ticket, autoSave: true);

            var activity = new TicketActivity(
                Guid.NewGuid(),
                ticket.Id,
                TicketActivityType.Assigned,
                description: $"Kỹ thuật viên {user.UserName} đã tiếp nhận vé trực tiếp qua nút bấm Discord."
            );
            await activityRepo.InsertAsync(activity, autoSave: true);

            // Thông báo trong hệ thống cho người nhận vé
            await notificationManager.CreateAsync(
                user.Id,
                NotificationType.TicketAssigned,
                "Tiếp nhận vé thành công",
                $"Bạn đã tiếp nhận xử lý vé {ticket.TicketNumber} \"{ticket.Title}\" qua Discord.",
                ticket.Id
            );

            // Thông báo cho người yêu cầu (nếu có tài khoản)
            if (ticket.RequesterId.HasValue && inProgressStatus != null)
            {
                await notificationManager.CreateAsync(
                    ticket.RequesterId.Value,
                    NotificationType.StatusChanged,
                    "Trạng thái vé đã thay đổi",
                    $"Vé {ticket.TicketNumber} \"{ticket.Title}\" đã được tiếp nhận và chuyển sang trạng thái \"{inProgressStatus.Name}\".",
                    ticket.Id
                );
            }

            await uow.CompleteAsync();

            // Cập nhật giao diện tin nhắn gốc trên Discord kèm nút [🏁 Hoàn thành vé]
            if (component.Message != null)
            {
                var oldEmbed = component.Message.Embeds.FirstOrDefault();
                var ticketUrl = BuildTicketUrl(ticket.Id);

                var updatedEmbed = new EmbedBuilder()
                    .WithTitle(oldEmbed?.Title ?? $"[{ticket.TicketNumber}] {ticket.Title}")
                    .WithDescription(oldEmbed?.Description ?? ticket.Description)
                    .WithUrl(ticketUrl)
                    .WithColor(new Color(0xf3, 0x9c, 0x12)) // Vàng cam: Đang xử lý
                    .WithFooter($"Tiếp nhận bởi @{user.UserName} lúc {DateTime.Now:HH:mm dd/MM/yyyy}", "https://abp.io/assets/png/abp-logo.png")
                    .WithCurrentTimestamp();

                if (oldEmbed != null)
                {
                    foreach (var field in oldEmbed.Fields)
                    {
                        if (field.Name.Contains("Kỹ thuật viên"))
                        {
                            updatedEmbed.AddField("👨‍💻 Kỹ thuật viên phụ trách", $"✅ {user.UserName}", inline: true);
                        }
                        else if (field.Name.Contains("Trạng thái"))
                        {
                            updatedEmbed.AddField("📌 Trạng thái", "Đang xử lý (In Progress)", inline: true);
                        }
                        else
                        {
                            updatedEmbed.AddField(field.Name, field.Value, inline: field.Inline);
                        }
                    }
                }

                var updatedComponents = new ComponentBuilder()
                    .WithButton($"✅ Đã nhận bởi {user.UserName}", "claimed_disabled", ButtonStyle.Secondary, disabled: true)
                    .WithButton("🏁 Hoàn thành vé", $"resolve_ticket_{ticket.Id}", ButtonStyle.Success)
                    .WithButton("👁️ Xem trên Web", style: ButtonStyle.Link, url: ticketUrl)
                    .Build();

                await component.UpdateAsync(msg =>
                {
                    msg.Embed = updatedEmbed.Build();
                    msg.Components = updatedComponents;
                });
            }

            await component.FollowupAsync(
                $"🎉 Tuyệt vời! Bạn (**{user.UserName}**) đã tiếp nhận sự vụ **[{ticket.TicketNumber}] {ticket.Title}** thành công.\n" +
                $"👉 Khi xử lý xong, bạn có thể bấm trực tiếp nút **[🏁 Hoàn thành vé]** trên tin nhắn Discord để đóng vé!",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý nút bấm nhận vé Discord cho Component: {CustomId}", component.Data.CustomId);
            try
            {
                await component.FollowupAsync("❌ Đã xảy ra lỗi khi xử lý thao tác nhận vé. Vui lòng thử lại trên web.", ephemeral: true);
            }
            catch { }
        }
    }

    private async Task OnModalSubmittedAsync(SocketModal modal)
    {
        try
        {
            if (!modal.Data.CustomId.StartsWith("resolve_modal_")) return;

            await modal.DeferAsync(ephemeral: true);

            var ticketIdStr = modal.Data.CustomId.Replace("resolve_modal_", "");
            if (!Guid.TryParse(ticketIdStr, out var ticketId))
            {
                await modal.FollowupAsync("❌ ID sự vụ không hợp lệ.", ephemeral: true);
                return;
            }

            var resolutionNote = modal.Data.Components
                .FirstOrDefault(x => x.CustomId == "resolution_note")?.Value?.Trim();

            using var scope = _serviceScopeFactory.CreateScope();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ticket, Guid>>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();
            var statusRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketStatus, Guid>>();
            var ticketManager = scope.ServiceProvider.GetRequiredService<TicketManager>();
            var commentRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketComment, Guid>>();
            var activityRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketActivity, Guid>>();
            var notificationManager = scope.ServiceProvider.GetRequiredService<NotificationManager>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using var uow = uowManager.Begin();

            var allUsers = await userRepo.GetListAsync();
            var discordUserIdStr = modal.User.Id.ToString();
            var user = allUsers.FirstOrDefault(u => u.GetProperty<string>("DiscordUserId") == discordUserIdStr)
                       ?? allUsers.FirstOrDefault(u => string.Equals(u.UserName, modal.User.Username, StringComparison.OrdinalIgnoreCase));

            var userName = user?.UserName ?? modal.User.Username;

            var ticket = await ticketRepo.FindAsync(ticketId);
            if (ticket == null)
            {
                await modal.FollowupAsync("❌ Không tìm thấy sự vụ này trên hệ thống.", ephemeral: true);
                return;
            }

            var allStatuses = await statusRepo.GetListAsync();
            var resolvedStatus = allStatuses.FirstOrDefault(s => s.Code == "RESOLVED" || s.Name.Contains("Resolved", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("Đã giải quyết", StringComparison.OrdinalIgnoreCase))
                              ?? allStatuses.FirstOrDefault(s => s.StatusGroup == StatusGroup.Closed || s.IsFinal);

            if (resolvedStatus == null)
            {
                await modal.FollowupAsync("❌ Không tìm thấy trạng thái 'Resolved' trong hệ thống.", ephemeral: true);
                return;
            }

            var oldStatus = allStatuses.FirstOrDefault(s => s.Id == ticket.StatusId);
            if (oldStatus != null)
            {
                await ticketManager.ChangeStatusAsync(ticket, oldStatus, resolvedStatus);
            }
            else
            {
                ticket.StatusId = resolvedStatus.Id;
            }

            ticket.ResolvedAt ??= DateTime.UtcNow;
            await ticketRepo.UpdateAsync(ticket, autoSave: true);

            // Lưu ghi chú xử lý vào comment nếu có
            if (!string.IsNullOrWhiteSpace(resolutionNote))
            {
                var comment = new TicketComment(
                    Guid.NewGuid(),
                    ticket.Id,
                    $"[Ghi chú giải quyết qua Discord]: {resolutionNote}",
                    isInternal: false
                );
                await commentRepo.InsertAsync(comment, autoSave: true);
            }

            var activity = new TicketActivity(
                Guid.NewGuid(),
                ticket.Id,
                TicketActivityType.Closed,
                description: $"Kỹ thuật viên {userName} đã đánh dấu hoàn thành sự vụ qua Discord." + (!string.IsNullOrWhiteSpace(resolutionNote) ? $" Ghi chú: {resolutionNote}" : "")
            );
            await activityRepo.InsertAsync(activity, autoSave: true);

            if (ticket.RequesterId.HasValue)
            {
                await notificationManager.CreateAsync(
                    ticket.RequesterId.Value,
                    NotificationType.StatusChanged,
                    "Sự vụ đã được giải quyết",
                    $"Sự vụ {ticket.TicketNumber} \"{ticket.Title}\" đã được kỹ thuật viên {userName} giải quyết thành công.",
                    ticket.Id
                );
            }

            await uow.CompleteAsync();

            // Cập nhật Embed trên tin nhắn gốc sang màu xanh lá
            if (modal.Message != null)
            {
                var oldEmbed = modal.Message.Embeds.FirstOrDefault();
                var ticketUrl = BuildTicketUrl(ticket.Id);

                var resolvedEmbed = new EmbedBuilder()
                    .WithTitle(oldEmbed?.Title ?? $"[{ticket.TicketNumber}] {ticket.Title}")
                    .WithDescription(oldEmbed?.Description ?? ticket.Description)
                    .WithUrl(ticketUrl)
                    .WithColor(new Color(0x2e, 0xcc, 0x71)) // Xanh lá: Resolved
                    .WithFooter($"Giải quyết bởi @{userName} lúc {DateTime.Now:HH:mm dd/MM/yyyy}", "https://abp.io/assets/png/abp-logo.png")
                    .WithCurrentTimestamp();

                if (oldEmbed != null)
                {
                    foreach (var field in oldEmbed.Fields)
                    {
                        if (field.Name.Contains("Trạng thái"))
                        {
                            resolvedEmbed.AddField("📌 Trạng thái", "✅ Đã giải quyết (Resolved)", inline: true);
                        }
                        else
                        {
                            resolvedEmbed.AddField(field.Name, field.Value, inline: field.Inline);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(resolutionNote))
                {
                    resolvedEmbed.AddField("📝 Ghi chú giải quyết", resolutionNote, inline: false);
                }

                var resolvedComponents = new ComponentBuilder()
                    .WithButton($"✅ Đã giải quyết bởi {userName}", "resolved_disabled", ButtonStyle.Success, disabled: true)
                    .WithButton("👁️ Xem trên Web", style: ButtonStyle.Link, url: ticketUrl)
                    .Build();

                await modal.Message.ModifyAsync(msg =>
                {
                    msg.Embed = resolvedEmbed.Build();
                    msg.Components = resolvedComponents;
                });
            }

            await modal.FollowupAsync(
                $"🎉 Tuyệt vời! Bạn đã đánh dấu hoàn thành sự vụ **[{ticket.TicketNumber}] {ticket.Title}** thành công.",
                ephemeral: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý Modal giải quyết sự vụ Discord cho CustomId: {CustomId}", modal.Data.CustomId);
            try
            {
                await modal.FollowupAsync("❌ Đã xảy ra lỗi khi hoàn thành sự vụ. Vui lòng kiểm tra lại trên web.", ephemeral: true);
            }
            catch { }
        }
    }

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
    {
        try
        {
            if (command.Data.Name == "link-helpdesk")
            {
                await HandleLinkHelpdeskCommandAsync(command);
                return;
            }

            if (command.Data.Name == "my-tickets")
            {
                await HandleMyTicketsCommandAsync(command);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi thực thi Slash Command: {CommandName}", command.Data.Name);
            try
            {
                await command.RespondAsync("❌ Đã xảy ra lỗi khi xử lý lệnh.", ephemeral: true);
            }
            catch { }
        }
    }

    private async Task OnMessageReceivedAsync(SocketMessage rawMsg)
    {
        if (rawMsg is not SocketUserMessage message || message.Author.IsBot)
        {
            return;
        }

        var content = message.Content.Trim();

        // 1. Lệnh xem vé đang nhận: !my-tickets, !tickets, !mytickets, hoặc /my-tickets (dạng text thông thường)
        if (content.Equals("!my-tickets", StringComparison.OrdinalIgnoreCase) ||
            content.Equals("!tickets", StringComparison.OrdinalIgnoreCase) ||
            content.Equals("!mytickets", StringComparison.OrdinalIgnoreCase) ||
            content.Equals("/my-tickets", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var (embed, text) = await GenerateMyTicketsResponseAsync(message.Author.Id, message.Author.Username);
                if (embed != null)
                {
                    await message.Channel.SendMessageAsync(text, embed: embed, messageReference: new MessageReference(message.Id));
                }
                else
                {
                    await message.Channel.SendMessageAsync(text ?? "❌ Không thể tải danh sách sự vụ.", messageReference: new MessageReference(message.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý lệnh tin nhắn !my-tickets");
            }
            return;
        }

        // 2. Lệnh liên kết tài khoản: !link <tên_đăng_nhập>
        if (content.StartsWith("!link", StringComparison.OrdinalIgnoreCase))
        {
            var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await message.Channel.SendMessageAsync("👉 Cú pháp đúng: `!link <tên_đăng_nhập_helpdesk>` (Ví dụ: `!link staff1`)", messageReference: new MessageReference(message.Id));
                return;
            }

            var username = parts[1];
            var result = await LinkUserCoreAsync(message.Author.Id, message.Author.Username, username);
            await message.Channel.SendMessageAsync(result, messageReference: new MessageReference(message.Id));
            return;
        }
    }

    private async Task HandleLinkHelpdeskCommandAsync(SocketSlashCommand command)
    {
        var usernameOpt = command.Data.Options.FirstOrDefault(o => o.Name == "username")?.Value?.ToString();
        if (string.IsNullOrWhiteSpace(usernameOpt))
        {
            await command.RespondAsync("❌ Vui lòng cung cấp tên đăng nhập Helpdesk.", ephemeral: true);
            return;
        }

        var result = await LinkUserCoreAsync(command.User.Id, command.User.Username, usernameOpt);
        await command.RespondAsync(result, ephemeral: true);
    }

    private async Task<string> LinkUserCoreAsync(ulong discordUserId, string discordUsername, string helpdeskUsername)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

        using var uow = uowManager.Begin();

        var allUsers = await userRepo.GetListAsync();
        var user = allUsers.FirstOrDefault(u => string.Equals(u.UserName, helpdeskUsername.Trim(), StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            return $"❌ Không tìm thấy nhân viên nào có tên đăng nhập **{helpdeskUsername}** trong hệ thống Helpdesk. Vui lòng kiểm tra lại!";
        }

        user.SetProperty("DiscordUserId", discordUserId.ToString());
        user.SetProperty("DiscordUsername", discordUsername);
        await userRepo.UpdateAsync(user, autoSave: true);

        await uow.CompleteAsync();

        return $"✅ Liên kết thành công!\n" +
               $"Tài khoản Discord **@{discordUsername}** đã được kết nối với nhân viên **{user.UserName}** ({user.Email}).\n" +
               $"👉 Bây giờ bạn có thể nhận vé và dùng lệnh **`/my-tickets`** hoặc gõ tin nhắn **`!my-tickets`** trên Discord!";
    }

    private async Task HandleMyTicketsCommandAsync(SocketSlashCommand command)
    {
        await command.DeferAsync(ephemeral: true);

        var (embed, text) = await GenerateMyTicketsResponseAsync(command.User.Id, command.User.Username);
        if (embed != null)
        {
            await command.FollowupAsync(text, embed: embed, ephemeral: true);
        }
        else
        {
            await command.FollowupAsync(text ?? "❌ Không thể tải danh sách sự vụ.", ephemeral: true);
        }
    }

    private async Task<(Embed? Embed, string? Message)> GenerateMyTicketsResponseAsync(ulong discordUserId, string discordUsername)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();
        var ticketRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ticket, Guid>>();
        var statusRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketStatus, Guid>>();
        var priorityRepo = scope.ServiceProvider.GetRequiredService<IRepository<Priority, Guid>>();

        var allUsers = await userRepo.GetListAsync();
        var discordUserIdStr = discordUserId.ToString();
        var user = allUsers.FirstOrDefault(u => u.GetProperty<string>("DiscordUserId") == discordUserIdStr)
                   ?? allUsers.FirstOrDefault(u => string.Equals(u.UserName, discordUsername, StringComparison.OrdinalIgnoreCase));

        if (user == null)
        {
            return (null,
                $"⚠️ Tài khoản Discord của bạn (**@{discordUsername}**) chưa được liên kết với nhân viên nào trong Helpdesk.\n" +
                $"👉 Bạn có thể liên kết nhanh bằng cách gõ: `!link <tên_đăng_nhập>` (hoặc `/link-helpdesk <tên_đăng_nhập>`).");
        }

        var allStatuses = await statusRepo.GetListAsync();
        var closedStatusIds = allStatuses
            .Where(s => s.StatusGroup == StatusGroup.Closed || s.IsFinal)
            .Select(s => s.Id)
            .ToHashSet();

        var statusDict = allStatuses.ToDictionary(s => s.Id, s => s.Name);

        var allPriorities = await priorityRepo.GetListAsync();
        var priorityDict = allPriorities.ToDictionary(p => p.Id, p => p.Name);

        var myActiveTickets = await ticketRepo.GetListAsync(t => t.AssigneeId == user.Id && !closedStatusIds.Contains(t.StatusId));

        var embed = new EmbedBuilder()
            .WithTitle($"📋 Danh Sách Sự Vụ Đang Nhận - @{user.UserName}")
            .WithColor(new Color(0x34, 0x98, 0xdb))
            .WithFooter($"Helpdesk ITSM System • Tổng cộng: {myActiveTickets.Count} vé", "https://abp.io/assets/png/abp-logo.png")
            .WithCurrentTimestamp();

        if (myActiveTickets.Count == 0)
        {
            embed.WithDescription("🎉 Hiện tại bạn không có sự vụ nào đang chờ xử lý. Tận hưởng ngày làm việc nhé!");
        }
        else
        {
            embed.WithDescription($"Bạn đang có **{myActiveTickets.Count}** sự vụ đang trong quá trình xử lý:");

            foreach (var t in myActiveTickets.OrderByDescending(x => x.CreationTime).Take(10))
            {
                var priName = priorityDict.GetValueOrDefault(t.PriorityId, "Bình thường");
                var statName = statusDict.GetValueOrDefault(t.StatusId, "Đang xử lý");
                var slaStr = t.DueDate.HasValue ? t.DueDate.Value.ToString("dd/MM/yyyy HH:mm") : "Chưa có";
                var ticketUrl = BuildTicketUrl(t.Id);

                var fieldContent = $"⚡ **Mức độ:** {priName} | 📌 **Trạng thái:** {statName}\n" +
                                   $"⏱️ **Hạn SLA:** {slaStr} • [👁️ Xem trên Web]({ticketUrl})";

                embed.AddField($"🎫 [{t.TicketNumber}] {t.Title}", fieldContent, inline: false);
            }

            if (myActiveTickets.Count > 10)
            {
                embed.AddField("...", $"*Còn {myActiveTickets.Count - 10} sự vụ khác trên giao diện Web.*", inline: false);
            }
        }

        return (embed.Build(), null);
    }

    private string BuildTicketUrl(Guid ticketId)
    {
        var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        return $"{angularUrl}/tickets/{ticketId}";
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
