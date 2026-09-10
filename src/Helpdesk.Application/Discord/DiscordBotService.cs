using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Helpdesk.AssignmentRules;
using Helpdesk.Categories;
using Helpdesk.Notifications;
using Helpdesk.Priorities;
using Helpdesk.Settings;
using Helpdesk.Sla;
using Helpdesk.Tickets;
using Helpdesk.TicketSources;
using Helpdesk.TicketStatuses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Http;
using Volo.Abp.BlobStoring;
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
        bool isCritical,
        ulong? discordUserId = null)
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

            var sentMsg = await channel.SendMessageAsync(embed: embed, components: components);
            _logger.LogInformation("Đã gửi thông báo vé [{TicketNumber}] kèm nút bấm vào kênh Discord {ChannelId}", ticketNumber, channelId);

            // Tự động tạo Discord Thread cho sự vụ này để đồng bộ bình luận 2 chiều
            try
            {
                if (channel is ITextChannel textChannel && sentMsg != null)
                {
                    var threadName = $"[{ticketNumber}] {title}";
                    if (threadName.Length > 95) threadName = threadName.Substring(0, 92) + "...";

                    var thread = await textChannel.CreateThreadAsync(
                        threadName,
                        autoArchiveDuration: ThreadArchiveDuration.ThreeDays,
                        message: sentMsg
                    );

                    if (thread != null)
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var ticketRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ticket, Guid>>();
                        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                        using var uow = uowManager.Begin();

                        var ticketToUpdate = await ticketRepo.FindAsync(ticketId);
                        if (ticketToUpdate != null)
                        {
                            ticketToUpdate.DiscordThreadId = thread.Id.ToString();
                            await ticketRepo.UpdateAsync(ticketToUpdate, autoSave: true);
                            await uow.CompleteAsync();
                        }

                        var userMention = discordUserId.HasValue ? $" <@{discordUserId.Value}>" : string.Empty;
                        await thread.SendMessageAsync(
                            $"🧵 **Luồng thảo luận cho sự vụ [{ticketNumber}] đã được kích hoạt!**\n" +
                            (discordUserId.HasValue ? $"Chào{userMention}! Bạn có thể thảo luận trực tiếp với đội ngũ kỹ thuật tại đây.\n" : string.Empty) +
                            $"📸 **Mẹo gửi hình ảnh**: Hãy dán (Ctrl+V) hoặc kéo thả trực tiếp ảnh chụp màn hình/tệp tin vào luồng này, hệ thống sẽ tự động đồng bộ vào mục Tệp đính kèm trên Web!\n" +
                            $"💬 Mọi tin nhắn trao đổi trong luồng này đều được đồng bộ 2 chiều với hệ thống Helpdesk."
                        );
                        _logger.LogInformation("Đã tạo Discord Thread {ThreadId} cho sự vụ {TicketNumber}", thread.Id, ticketNumber);
                    }
                }
            }
            catch (Exception threadEx)
            {
                _logger.LogWarning(threadEx, "Không thể tự động tạo Discord Thread cho vé {TicketNumber}", ticketNumber);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi tin nhắn kèm nút tới Discord cho vé {TicketNumber}", ticketNumber);
            return false;
        }
    }

    public async Task<bool> SendMessageToThreadAsync(ulong threadId, string authorName, string content)
    {
        if (_client == null || _client.ConnectionState != ConnectionState.Connected)
        {
            return false;
        }

        try
        {
            var channel = await _client.GetChannelAsync(threadId);
            if (channel is IThreadChannel thread)
            {
                await thread.SendMessageAsync($"💬 **[{authorName}]**: {content}");
                _logger.LogInformation("Đã chuyển tiếp bình luận từ Web vào Discord Thread {ThreadId}", threadId);
                return true;
            }

            _logger.LogWarning("Không tìm thấy Discord Thread với ID: {ThreadId}", threadId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi gửi tin nhắn vào Discord Thread {ThreadId}", threadId);
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

                // 3. Đăng ký Slash Command: /create-ticket
                var createTicketCommand = new SlashCommandBuilder()
                    .WithName("create-ticket")
                    .WithDescription("Tạo một yêu cầu hỗ trợ (ticket) mới vào hệ thống Helpdesk");

                var linkBuilt = linkCommand.Build();
                var myTicketsBuilt = myTicketsCommand.Build();
                var createTicketBuilt = createTicketCommand.Build();

                // Đăng ký trực tiếp cho từng Guild (Server) để hiển thị NGAY LẬP TỨC trên Discord
                if (_client != null)
                {
                    foreach (var guild in _client.Guilds)
                    {
                        try
                        {
                            await guild.CreateApplicationCommandAsync(linkBuilt);
                            await guild.CreateApplicationCommandAsync(myTicketsBuilt);
                            await guild.CreateApplicationCommandAsync(createTicketBuilt);
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
                    await _client.CreateGlobalApplicationCommandAsync(createTicketBuilt);
                }

                _logger.LogInformation("Đã hoàn tất đăng ký Slash Command (/link-helpdesk, /my-tickets, /create-ticket) cấp Guild và Global.");
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
                if (ticket.AssigneeId.Value == user.Id)
                {
                    await component.FollowupAsync(
                        $"ℹ️ Bạn (**{user.UserName}**) đã tiếp nhận sự vụ **[{ticket.TicketNumber}]** này trước đó rồi.",
                        ephemeral: true);
                }
                else
                {
                    await component.FollowupAsync(
                        $"⚠️ Vé **[{ticket.TicketNumber}]** đã được tiếp nhận trước đó bởi kỹ thuật viên **{currentAssignee?.UserName ?? "khác"}**!",
                        ephemeral: true);
                }
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
                try
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

                    await component.Message.ModifyAsync(msg =>
                    {
                        msg.Embed = updatedEmbed.Build();
                        msg.Components = updatedComponents;
                    });
                }
                catch (Exception mEx)
                {
                    _logger.LogWarning(mEx, "Không thể cập nhật giao diện tin nhắn Discord sau khi nhận vé: {Message}", mEx.Message);
                }
            }

            // Thông báo vào Thread nếu có
            if (!string.IsNullOrWhiteSpace(ticket.DiscordThreadId) && ulong.TryParse(ticket.DiscordThreadId, out var threadId) && _client != null)
            {
                try
                {
                    var thread = await _client.GetChannelAsync(threadId) as IThreadChannel;
                    if (thread != null)
                    {
                        await thread.SendMessageAsync($"👨‍💻 Kỹ thuật viên **@{user.UserName}** đã tiếp nhận xử lý sự vụ **[{ticket.TicketNumber}]**.");
                    }
                }
                catch { }
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
            if (modal.Data.CustomId == "discord_create_ticket_modal")
            {
                await HandleCreateTicketModalSubmittedAsync(modal);
                return;
            }

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

            // Cập nhật Embed trên tin nhắn gốc sang màu xanh lá (nếu có thể)
            if (modal.Message != null)
            {
                try
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
                catch (Exception mEx)
                {
                    _logger.LogInformation("Không thể sửa tin nhắn gốc (thường xảy ra với tin nhắn ephemeral): {Message}", mEx.Message);
                }
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

            if (command.Data.Name == "create-ticket")
            {
                await HandleCreateTicketCommandAsync(command);
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

        // 0. Kiểm tra nếu tin nhắn được gửi trong một Thread thảo luận của Ticket (đồng bộ vào Web)
        if (message.Channel is SocketThreadChannel threadChannel)
        {
            await HandleThreadMessageAsync(threadChannel, message);
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
                var (embed, components, text) = await GenerateMyTicketsResponseAsync(message.Author.Id, message.Author.Username);
                if (embed != null)
                {
                    await message.Channel.SendMessageAsync(text, embed: embed, components: components, messageReference: new MessageReference(message.Id));
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

        var (embed, components, text) = await GenerateMyTicketsResponseAsync(command.User.Id, command.User.Username);
        if (embed != null)
        {
            await command.FollowupAsync(text, embed: embed, components: components, ephemeral: true);
        }
        else
        {
            await command.FollowupAsync(text ?? "❌ Không thể tải danh sách sự vụ.", ephemeral: true);
        }
    }

    private async Task<(Embed? Embed, MessageComponent? Components, string? Message)> GenerateMyTicketsResponseAsync(ulong discordUserId, string discordUsername)
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
            return (null, null,
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
            embed.WithDescription($"Bạn đang có **{myActiveTickets.Count}** sự vụ đang trong quá trình xử lý:\n*Bấm các nút bên dưới để đánh dấu hoàn thành trực tiếp trên Discord:*");

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

        // Tạo nút Hoàn thành vé cho tối đa 5 vé đang xử lý (mỗi vé 1 hàng gồm nút Hoàn thành + Link Web)
        var compBuilder = new ComponentBuilder();
        var row = 0;
        foreach (var t in myActiveTickets.OrderByDescending(x => x.CreationTime).Take(5))
        {
            compBuilder.WithButton(
                $"🏁 Hoàn thành {t.TicketNumber}",
                $"resolve_ticket_{t.Id}",
                ButtonStyle.Success,
                row: row
            );
            compBuilder.WithButton(
                "👁️ Xem trên Web",
                style: ButtonStyle.Link,
                url: BuildTicketUrl(t.Id),
                row: row
            );
            row++;
        }

        var components = myActiveTickets.Count > 0 ? compBuilder.Build() : null;
        return (embed.Build(), components, null);
    }

    private string BuildTicketUrl(Guid ticketId)
    {
        var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        return $"{angularUrl}/tickets/{ticketId}";
    }

    private async Task HandleCreateTicketCommandAsync(SocketSlashCommand command)
    {
        var mb = new ModalBuilder()
            .WithTitle("🎫 Tạo Yêu Cầu Hỗ Trợ Mới")
            .WithCustomId("discord_create_ticket_modal")
            .AddTextInput(
                label: "Tiêu đề sự cố / yêu cầu",
                customId: "ticket_title",
                placeholder: "Ví dụ: Lỗi không vào được phần mềm, hỏng mạng...",
                required: true,
                maxLength: 200
            )
            .AddTextInput(
                label: "Danh mục (Phần mềm, Mạng, Phần cứng...)",
                customId: "ticket_category",
                placeholder: "Nhập danh mục sự cố (hoặc để trống nếu chưa rõ)",
                required: false,
                maxLength: 100
            )
            .AddTextInput(
                label: "Mô tả chi tiết sự cố",
                customId: "ticket_description",
                placeholder: "Mô tả chi tiết hiện tượng lỗi, các bước gặp lỗi...",
                style: TextInputStyle.Paragraph,
                required: true,
                maxLength: 2000
            );

        await command.RespondWithModalAsync(mb.Build());
    }

    private async Task HandleCreateTicketModalSubmittedAsync(SocketModal modal)
    {
        await modal.DeferAsync(ephemeral: true);

        try
        {
            var components = modal.Data.Components.ToList();
            var title = components.FirstOrDefault(x => x.CustomId == "ticket_title")?.Value?.Trim();
            var categoryText = components.FirstOrDefault(x => x.CustomId == "ticket_category")?.Value?.Trim();
            var description = components.FirstOrDefault(x => x.CustomId == "ticket_description")?.Value?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                await modal.FollowupAsync("❌ Tiêu đề sự vụ không được để trống.", ephemeral: true);
                return;
            }

            using var scope = _serviceScopeFactory.CreateScope();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ticket, Guid>>();
            var categoryRepo = scope.ServiceProvider.GetRequiredService<IRepository<Category, Guid>>();
            var priorityRepo = scope.ServiceProvider.GetRequiredService<IRepository<Priority, Guid>>();
            var statusRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketStatus, Guid>>();
            var sourceRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketSource, Guid>>();
            var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();
            var ticketManager = scope.ServiceProvider.GetRequiredService<TicketManager>();
            var slaManager = scope.ServiceProvider.GetRequiredService<SlaManager>();
            var autoAssignmentManager = scope.ServiceProvider.GetRequiredService<AutoAssignmentManager>();
            var activityRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketActivity, Guid>>();
            var settingProvider = scope.ServiceProvider.GetRequiredService<ISettingProvider>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            using var uow = uowManager.Begin();

            // 1. Phân giải người gửi (kiểm tra link tài khoản Helpdesk)
            var discordUserIdStr = modal.User.Id.ToString();
            var allUsers = await userRepo.GetListAsync();
            var linkedUser = allUsers.FirstOrDefault(u => u.GetProperty<string>("DiscordUserId") == discordUserIdStr)
                             ?? allUsers.FirstOrDefault(u => string.Equals(u.UserName, modal.User.Username, StringComparison.OrdinalIgnoreCase));

            var requesterName = linkedUser?.UserName ?? $"{modal.User.Username} (Discord)";
            var requesterEmail = linkedUser?.Email ?? $"{modal.User.Username.ToLower()}@discord.user";
            Guid? requesterId = linkedUser?.Id;

            // 2. Tìm danh mục phù hợp
            var allCategories = await categoryRepo.GetListAsync();
            Category? category = null;
            if (!string.IsNullOrWhiteSpace(categoryText))
            {
                category = allCategories.FirstOrDefault(c => c.Name.Contains(categoryText, StringComparison.OrdinalIgnoreCase));
            }
            category ??= allCategories.FirstOrDefault();
            if (category == null)
            {
                await modal.FollowupAsync("❌ Chưa có danh mục nào được cấu hình trong hệ thống.", ephemeral: true);
                return;
            }

            // 3. Tìm mức ưu tiên mặc định (Normal / Medium)
            var allPriorities = await priorityRepo.GetListAsync();
            var priority = allPriorities.FirstOrDefault(p => p.Code == "NORMAL" || p.Code == "MEDIUM" || p.Name.Contains("Normal", StringComparison.OrdinalIgnoreCase))
                           ?? allPriorities.FirstOrDefault();
            if (priority == null)
            {
                await modal.FollowupAsync("❌ Chưa có mức ưu tiên nào được cấu hình trong hệ thống.", ephemeral: true);
                return;
            }

            // 4. Tìm trạng thái mặc định (Open)
            var allStatuses = await statusRepo.GetListAsync();
            var status = allStatuses.FirstOrDefault(s => s.Code == "OPEN" || s.Name.Contains("Open", StringComparison.OrdinalIgnoreCase))
                         ?? allStatuses.FirstOrDefault();
            if (status == null)
            {
                await modal.FollowupAsync("❌ Chưa có trạng thái nào được cấu hình trong hệ thống.", ephemeral: true);
                return;
            }

            // 5. Tìm nguồn Discord
            var allSources = await sourceRepo.GetListAsync();
            var source = allSources.FirstOrDefault(s => s.Code == "DISCORD" || s.Name.Contains("Discord", StringComparison.OrdinalIgnoreCase))
                         ?? allSources.FirstOrDefault();
            if (source == null)
            {
                await modal.FollowupAsync("❌ Chưa có nguồn sự vụ nào được cấu hình trong hệ thống.", ephemeral: true);
                return;
            }

            // 6. Tạo vé qua TicketManager
            var ticket = await ticketManager.CreateAsync(
                title: title,
                description: description,
                categoryId: category.Id,
                priorityId: priority.Id,
                statusId: status.Id,
                sourceId: source.Id,
                requesterName: requesterName,
                requesterEmail: requesterEmail,
                requesterId: requesterId
            );

            // Tính toán SLA hạn chót và tự động phân công
            await slaManager.CalculateSlaDatesAsync(ticket);
            await autoAssignmentManager.TryAssignTicketAsync(ticket);

            await ticketRepo.InsertAsync(ticket, autoSave: true);

            var activity = new TicketActivity(
                Guid.NewGuid(),
                ticket.Id,
                TicketActivityType.Created,
                description: $"Sự vụ được tạo trực tiếp từ Discord bởi @{modal.User.Username}."
            );
            await activityRepo.InsertAsync(activity, autoSave: true);

            await uow.CompleteAsync();

            var ticketUrl = BuildTicketUrl(ticket.Id);
            await modal.FollowupAsync(
                $"🎉 **Yêu cầu hỗ trợ của bạn đã được ghi nhận thành công!**\n" +
                $"• Mã sự vụ: **{ticket.TicketNumber}**\n" +
                $"• Tiêu đề: **{ticket.Title}**\n" +
                $"• Danh mục: **{category.Name}** | Hạn chót SLA: **{ticket.DueDate?.ToString("HH:mm dd/MM/yyyy") ?? "Đang tính"}**\n" +
                $"📸 **Đính kèm hình ảnh**: Hãy mở **Luồng thảo luận (Thread)** vừa tạo và dán (Ctrl+V) hoặc kéo thả ảnh chụp màn hình vào, hệ thống sẽ tự động đồng bộ vào mục Tệp đính kèm trên Web!\n" +
                $"🔗 [Xem chi tiết trên Web]({ticketUrl})",
                ephemeral: true);

            // Bắn thông báo lên kênh Discord kèm nút bấm và tự động tạo Thread
            var channelIdStr = await settingProvider.GetOrNullAsync(HelpdeskSettings.Discord.ChannelId);
            if (!string.IsNullOrWhiteSpace(channelIdStr) && ulong.TryParse(channelIdStr.Trim(), out var channelId))
            {
                var isCritical = priority.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase);
                await SendTicketWithButtonsAsync(
                    channelId,
                    ticket.Id,
                    ticket.TicketNumber,
                    ticket.Title,
                    ticket.Description,
                    requesterName,
                    ticket.DueDate,
                    category.Name,
                    priority.Name,
                    isCritical,
                    discordUserId: modal.User.Id
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo sự vụ từ Discord Modal");
            try
            {
                await modal.FollowupAsync("❌ Đã xảy ra lỗi khi tạo sự vụ qua Discord. Vui lòng thử lại sau.", ephemeral: true);
            }
            catch { }
        }
    }

    private async Task HandleThreadMessageAsync(SocketThreadChannel threadChannel, SocketUserMessage message)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var ticketRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ticket, Guid>>();
            var commentRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketComment, Guid>>();
            var activityRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketActivity, Guid>>();
            var attachmentRepo = scope.ServiceProvider.GetRequiredService<IRepository<TicketAttachment, Guid>>();
            var blobContainer = scope.ServiceProvider.GetRequiredService<IBlobContainer>();
            var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            var threadIdStr = threadChannel.Id.ToString();
            using var uow = uowManager.Begin();

            var ticket = await ticketRepo.FirstOrDefaultAsync(t => t.DiscordThreadId == threadIdStr);
            if (ticket == null)
            {
                return;
            }

            var authorName = message.Author.Username;
            var hasAttachments = message.Attachments != null && message.Attachments.Any();
            var content = message.Content?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(content) && hasAttachments)
            {
                content = $"[Đã gửi {message.Attachments.Count} tệp/hình ảnh đính kèm từ Discord]";
            }
            else if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var comment = new TicketComment(
                Guid.NewGuid(),
                ticket.Id,
                content,
                isInternal: false,
                authorName: $"{authorName} (Discord)"
            );

            await commentRepo.InsertAsync(comment, autoSave: true);

            // Đồng bộ tải các hình ảnh hoặc tệp tin đính kèm từ Discord vào Blob Storage & AppTicketAttachments
            if (hasAttachments && message.Attachments != null)
            {
                using var httpClient = new HttpClient();
                foreach (var att in message.Attachments)
                {
                    try
                    {
                        var fileBytes = await httpClient.GetByteArrayAsync(att.Url);
                        var ext = Path.GetExtension(att.Filename);
                        if (string.IsNullOrWhiteSpace(ext))
                        {
                            ext = ".png";
                        }

                        var blobName = $"{ticket.Id}/{Guid.NewGuid():N}{ext}";
                        await blobContainer.SaveAsync(blobName, fileBytes, overrideExisting: true);

                        var safeFileName = att.Filename.Length > 250 ? att.Filename.Substring(0, 245) + ext : att.Filename;
                        var safeContentType = string.IsNullOrWhiteSpace(att.ContentType) ? "application/octet-stream" : (att.ContentType.Length > 120 ? att.ContentType.Substring(0, 120) : att.ContentType);

                        var ticketAttachment = new TicketAttachment(
                            Guid.NewGuid(),
                            ticket.Id,
                            safeFileName,
                            fileBytes.Length,
                            safeContentType,
                            blobName,
                            commentId: comment.Id
                        );

                        await attachmentRepo.InsertAsync(ticketAttachment, autoSave: true);

                        var attActivity = new TicketActivity(
                            Guid.NewGuid(),
                            ticket.Id,
                            TicketActivityType.AttachmentAdded,
                            "Attachment",
                            null,
                            safeFileName,
                            $"@{authorName} đã gửi tệp đính kèm từ Discord: {safeFileName}"
                        );
                        await activityRepo.InsertAsync(attActivity, autoSave: true);
                    }
                    catch (Exception attEx)
                    {
                        _logger.LogWarning(attEx, "Không thể tải hoặc lưu tệp đính kèm {FileName} từ Discord", att.Filename);
                    }
                }
            }

            var activity = new TicketActivity(
                Guid.NewGuid(),
                ticket.Id,
                TicketActivityType.CommentAdded,
                description: $"Phản hồi mới từ @{authorName} qua luồng thảo luận Discord."
            );
            await activityRepo.InsertAsync(activity, autoSave: true);

            await uow.CompleteAsync();

            try
            {
                await message.AddReactionAsync(new Emoji("✅"));
                if (hasAttachments)
                {
                    await message.AddReactionAsync(new Emoji("📎"));
                }
            }
            catch { }

            _logger.LogInformation("Đã đồng bộ tin nhắn & tệp đính kèm từ Discord Thread {ThreadId} vào sự vụ {TicketNumber}", threadChannel.Id, ticket.TicketNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi đồng bộ tin nhắn từ Discord Thread vào sự vụ");
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
