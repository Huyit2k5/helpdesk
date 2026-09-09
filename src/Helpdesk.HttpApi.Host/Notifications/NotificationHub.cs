using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.SignalR;

namespace Helpdesk.Notifications;

/// <summary>
/// Hub SignalR đẩy thông báo real-time tới đúng người dùng đang đăng nhập (Clients.User theo Guid Id).
/// </summary>
[Authorize]
public class NotificationHub : AbpHub
{
}
