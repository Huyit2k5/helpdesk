using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace Helpdesk.Notifications;

[Authorize]
public class NotificationAppService : ApplicationService, INotificationAppService
{
    private readonly IRepository<Notification, Guid> _notificationRepository;

    public NotificationAppService(IRepository<Notification, Guid> notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(GetNotificationListInput input)
    {
        var currentUserId = CurrentUser.GetId();

        var queryable = (await _notificationRepository.GetQueryableAsync())
            .Where(n => n.RecipientUserId == currentUserId);

        if (input.IsRead.HasValue)
        {
            queryable = queryable.Where(n => n.IsRead == input.IsRead.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable
                .OrderByDescending(n => n.CreationTime)
                .PageBy(input.SkipCount, input.MaxResultCount)
        );

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResultDto<NotificationDto>(totalCount, dtos);
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var currentUserId = CurrentUser.GetId();
        var queryable = (await _notificationRepository.GetQueryableAsync())
            .Where(n => n.RecipientUserId == currentUserId && !n.IsRead);

        return await AsyncExecuter.CountAsync(queryable);
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var currentUserId = CurrentUser.GetId();
        var notification = await _notificationRepository.GetAsync(id);

        if (notification.RecipientUserId != currentUserId)
        {
            throw new BusinessException("Helpdesk:AccessDenied", "Bạn không có quyền truy cập thông báo này.");
        }

        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification, autoSave: true);
    }

    public async Task MarkAllAsReadAsync()
    {
        var currentUserId = CurrentUser.GetId();
        var queryable = (await _notificationRepository.GetQueryableAsync())
            .Where(n => n.RecipientUserId == currentUserId && !n.IsRead);

        var unread = await AsyncExecuter.ToListAsync(queryable);
        foreach (var notification in unread)
        {
            notification.MarkAsRead();
            await _notificationRepository.UpdateAsync(notification);
        }
    }

    private static NotificationDto MapToDto(Notification n)
    {
        return new NotificationDto
        {
            Id = n.Id,
            Type = n.Type.ToString(),
            Title = n.Title,
            Message = n.Message,
            TicketId = n.TicketId,
            IsRead = n.IsRead,
            ReadTime = n.ReadTime,
            CreationTime = n.CreationTime
        };
    }
}
