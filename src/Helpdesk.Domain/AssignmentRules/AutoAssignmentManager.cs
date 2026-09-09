using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Notifications;
using Helpdesk.Tickets;
using Helpdesk.TicketStatuses;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Identity;

namespace Helpdesk.AssignmentRules;

/// <summary>
/// Động cơ tự động điều phối và phân công sự vụ cho kỹ thuật viên.
/// </summary>
public class AutoAssignmentManager : DomainService
{
    private readonly IRepository<AssignmentRule, Guid> _ruleRepository;
    private readonly IRepository<AssignmentRuleAgent, Guid> _agentRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketStatus, Guid> _statusRepository;
    private readonly IRepository<TicketActivity, Guid> _activityRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly NotificationManager _notificationManager;

    public AutoAssignmentManager(
        IRepository<AssignmentRule, Guid> ruleRepository,
        IRepository<AssignmentRuleAgent, Guid> agentRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketStatus, Guid> statusRepository,
        IRepository<TicketActivity, Guid> activityRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<Department, Guid> departmentRepository,
        NotificationManager notificationManager)
    {
        _ruleRepository = ruleRepository;
        _agentRepository = agentRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _activityRepository = activityRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _notificationManager = notificationManager;
    }

    /// <summary>
    /// Thử tự động phân công sự vụ dựa trên các quy tắc cấu hình.
    /// </summary>
    /// <returns>True nếu phân công thành công, ngược lại False</returns>
    public async Task<bool> TryAssignTicketAsync(Ticket ticket)
    {
        // Nếu vé đã có người phụ trách thì không ghi đè
        if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != Guid.Empty)
        {
            return false;
        }

        var ruleQueryable = await _ruleRepository.GetQueryableAsync();
        var activeRules = ruleQueryable
            .Where(r => r.IsActive)
            .OrderBy(r => r.Order)
            .ToList();

        if (!activeRules.Any())
        {
            return false;
        }

        foreach (var rule in activeRules)
        {
            // Kiểm tra điều kiện Danh mục
            if (rule.CategoryId.HasValue && rule.CategoryId.Value != Guid.Empty && rule.CategoryId.Value != ticket.CategoryId)
            {
                continue;
            }

            // Kiểm tra điều kiện Mức ưu tiên
            if (rule.PriorityId.HasValue && rule.PriorityId.Value != Guid.Empty && rule.PriorityId.Value != ticket.PriorityId)
            {
                continue;
            }

            // Kiểm tra điều kiện Kênh tiếp nhận
            if (rule.SourceId.HasValue && rule.SourceId.Value != Guid.Empty && rule.SourceId.Value != ticket.SourceId)
            {
                continue;
            }

            // Thực thi phân bổ theo chiến lược
            Guid? chosenUserId = null;
            string strategyDescription = string.Empty;

            if (rule.RoutingStrategy == AssignmentStrategy.DirectAssign)
            {
                if (rule.DirectAssigneeId.HasValue)
                {
                    chosenUserId = rule.DirectAssigneeId.Value;
                    strategyDescription = "Chỉ định cố định";
                }
            }
            else
            {
                var agentQueryable = await _agentRepository.GetQueryableAsync();
                var agents = agentQueryable
                    .Where(a => a.RuleId == rule.Id)
                    .OrderBy(a => a.Order)
                    .ThenBy(a => a.Id)
                    .ToList();

                if (!agents.Any())
                {
                    continue;
                }

                if (rule.RoutingStrategy == AssignmentStrategy.RoundRobin)
                {
                    strategyDescription = "Xoay vòng (Round-Robin)";
                    int nextIndex = 0;
                    if (rule.LastAssignedUserId.HasValue)
                    {
                        var lastIndex = agents.FindIndex(a => a.UserId == rule.LastAssignedUserId.Value);
                        if (lastIndex >= 0)
                        {
                            nextIndex = (lastIndex + 1) % agents.Count;
                        }
                    }

                    chosenUserId = agents[nextIndex].UserId;
                    rule.LastAssignedUserId = chosenUserId;
                    await _ruleRepository.UpdateAsync(rule, autoSave: true);
                }
                else if (rule.RoutingStrategy == AssignmentStrategy.LeastBusy)
                {
                    strategyDescription = "Cân bằng tải (Least Busy)";
                    
                    // Lấy danh sách ID các trạng thái đã đóng / kết thúc
                    var statusQueryable = await _statusRepository.GetQueryableAsync();
                    var closedStatusIds = statusQueryable
                        .Where(s => s.StatusGroup == StatusGroup.Closed || s.IsFinal)
                        .Select(s => s.Id)
                        .ToList();

                    var candidateUserIds = agents.Select(a => a.UserId).Distinct().ToList();
                    var ticketQueryable = await _ticketRepository.GetQueryableAsync();

                    // Đếm số vé đang mở của từng kỹ thuật viên
                    var openTickets = ticketQueryable
                        .Where(t => t.AssigneeId.HasValue && candidateUserIds.Contains(t.AssigneeId.Value) && !closedStatusIds.Contains(t.StatusId))
                        .GroupBy(t => t.AssigneeId!.Value)
                        .Select(g => new { UserId = g.Key, OpenCount = g.Count() })
                        .ToDictionary(x => x.UserId, x => x.OpenCount);

                    // Chọn kỹ thuật viên có số vé mở ít nhất
                    chosenUserId = candidateUserIds
                        .OrderBy(uid => openTickets.GetValueOrDefault(uid, 0))
                        .FirstOrDefault();
                }
            }

            if (chosenUserId.HasValue)
            {
                ticket.AssigneeId = chosenUserId.Value;

                if (rule.DepartmentId.HasValue && (!ticket.DepartmentId.HasValue || ticket.DepartmentId.Value == Guid.Empty))
                {
                    ticket.DepartmentId = rule.DepartmentId.Value;
                }

                var user = await _userRepository.FindAsync(chosenUserId.Value);
                var userName = user?.Name ?? user?.UserName ?? "Kỹ thuật viên";

                var desc = $"Hệ thống tự động phân công vé cho {userName} theo quy tắc '{rule.Name}' ({strategyDescription}).";

                var activity = new TicketActivity(
                    GuidGenerator.Create(),
                    ticket.Id,
                    TicketActivityType.Assigned,
                    fieldName: "Assignee",
                    newVal: userName,
                    description: desc
                );

                await _activityRepository.InsertAsync(activity, autoSave: true);

                await _notificationManager.CreateAsync(
                    chosenUserId.Value,
                    NotificationType.TicketAssigned,
                    "Bạn được giao vé mới",
                    $"Vé {ticket.TicketNumber} \"{ticket.Title}\" vừa được tự động phân công cho bạn theo quy tắc '{rule.Name}'.",
                    ticket.Id
                );

                return true;
            }
        }

        return false;
    }
}
