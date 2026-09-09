using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.EventBus.Local;

namespace Helpdesk.Notifications;

/// <summary>
/// Tạo thông báo trong-ứng-dụng và phát sự kiện cục bộ để tầng Host đẩy real-time qua SignalR.
/// </summary>
public class NotificationManager : DomainService
{
    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly ILocalEventBus _localEventBus;

    public NotificationManager(
        IRepository<Notification, Guid> notificationRepository,
        ILocalEventBus localEventBus)
    {
        _notificationRepository = notificationRepository;
        _localEventBus = localEventBus;
    }

    public async Task<Notification> CreateAsync(
        Guid recipientUserId,
        NotificationType type,
        string title,
        string message,
        Guid? ticketId = null)
    {
        var notification = new Notification(
            GuidGenerator.Create(),
            recipientUserId,
            type,
            title,
            message,
            ticketId
        );

        await _notificationRepository.InsertAsync(notification, autoSave: true);

        await _localEventBus.PublishAsync(new NotificationCreatedEto
        {
            Id = notification.Id,
            RecipientUserId = recipientUserId,
            Type = type.ToString(),
            Title = title,
            Message = message,
            TicketId = ticketId,
            CreationTime = notification.CreationTime
        });

        return notification;
    }
}
