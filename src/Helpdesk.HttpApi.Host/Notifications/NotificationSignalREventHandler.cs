using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.EventBus;

namespace Helpdesk.Notifications;

/// <summary>
/// Lắng nghe sự kiện cục bộ khi có thông báo mới và đẩy real-time cho người dùng qua SignalR.
/// Tách riêng khỏi NotificationManager (Domain) để Domain không phụ thuộc AspNetCore/SignalR.
/// </summary>
public class NotificationSignalREventHandler : ILocalEventHandler<NotificationCreatedEto>, Volo.Abp.DependencyInjection.ITransientDependency
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationSignalREventHandler(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task HandleEventAsync(NotificationCreatedEto eventData)
    {
        await _hubContext.Clients
            .User(eventData.RecipientUserId.ToString())
            .SendAsync("notificationReceived", new
            {
                id = eventData.Id,
                type = eventData.Type,
                title = eventData.Title,
                message = eventData.Message,
                ticketId = eventData.TicketId,
                creationTime = eventData.CreationTime
            });
    }
}
