using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Helpdesk.Discord;

public interface IDiscordBotService
{
    bool IsConnected { get; }
    Task StartAsync();
    Task StopAsync();

    Task<bool> SendTicketWithButtonsAsync(
        ulong channelId,
        Guid ticketId,
        string ticketNumber,
        string title,
        string? description,
        string requesterName,
        DateTime? dueDate,
        string categoryName,
        string priorityName,
        bool isCritical);

    Task<bool> SendEmbedMessageAsync(
        ulong channelId,
        string title,
        string description,
        string? url,
        int color,
        List<object> fields);

    Task<bool> SendMessageToThreadAsync(ulong threadId, string authorName, string content);
}
