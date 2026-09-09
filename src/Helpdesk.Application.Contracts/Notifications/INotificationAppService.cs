using System;
using System.Threading.Tasks;
using Helpdesk.Notifications.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Notifications;

public interface INotificationAppService : IApplicationService
{
    Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(GetNotificationListInput input);

    Task<int> GetUnreadCountAsync();

    Task MarkAsReadAsync(Guid id);

    Task MarkAllAsReadAsync();
}
