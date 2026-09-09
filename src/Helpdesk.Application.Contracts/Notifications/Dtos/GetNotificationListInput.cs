using Volo.Abp.Application.Dtos;

namespace Helpdesk.Notifications.Dtos;

public class GetNotificationListInput : PagedAndSortedResultRequestDto
{
    public bool? IsRead { get; set; }
}
