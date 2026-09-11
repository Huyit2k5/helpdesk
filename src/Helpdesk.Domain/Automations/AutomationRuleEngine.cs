using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.Departments;
using Helpdesk.Discord;
using Helpdesk.Notifications;
using Helpdesk.Priorities;
using Helpdesk.Tickets;
using Helpdesk.TicketStatuses;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Identity;
using Volo.Abp.Linq;
using Volo.Abp.Timing;

namespace Helpdesk.Automations;

public class AutomationRuleEngine : DomainService, IAutomationRuleEngine, ITransientDependency
{
    private readonly IRepository<AutomationRule, Guid> _ruleRepository;
    private readonly IRepository<Macro, Guid> _macroRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketStatus, Guid> _statusRepository;
    private readonly IRepository<Priority, Guid> _priorityRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<TicketComment, Guid> _commentRepository;
    private readonly IRepository<TicketActivity, Guid> _activityRepository;
    private readonly TicketManager _ticketManager;
    private readonly NotificationManager _notificationManager;
    private readonly IDiscordNotificationService _discordNotificationService;
    private readonly IClock _clock;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly ILogger<AutomationRuleEngine> _logger;

    public AutomationRuleEngine(
        IRepository<AutomationRule, Guid> ruleRepository,
        IRepository<Macro, Guid> macroRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketStatus, Guid> statusRepository,
        IRepository<Priority, Guid> priorityRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<TicketComment, Guid> commentRepository,
        IRepository<TicketActivity, Guid> activityRepository,
        TicketManager ticketManager,
        NotificationManager notificationManager,
        IDiscordNotificationService discordNotificationService,
        IClock clock,
        IAsyncQueryableExecuter asyncExecuter,
        ILogger<AutomationRuleEngine> logger)
    {
        _ruleRepository = ruleRepository;
        _macroRepository = macroRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _priorityRepository = priorityRepository;
        _categoryRepository = categoryRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _commentRepository = commentRepository;
        _activityRepository = activityRepository;
        _ticketManager = ticketManager;
        _notificationManager = notificationManager;
        _discordNotificationService = discordNotificationService;
        _clock = clock;
        _asyncExecuter = asyncExecuter;
        _logger = logger;
    }

    public async Task<bool> ExecuteTriggersAsync(Ticket ticket, AutomationTriggerType triggerType, string? contextComment = null)
    {
        var rules = (await _ruleRepository.GetListAsync(r => r.IsActive && r.TriggerType == triggerType))
            .OrderBy(r => r.ExecutionOrder)
            .ToList();

        if (rules.Count == 0)
        {
            return false;
        }

        bool anyExecuted = false;
        var now = _clock.Now;

        foreach (var rule in rules)
        {
            var conditions = rule.GetConditions();
            if (EvaluateConditions(ticket, conditions, now, contextComment))
            {
                _logger.LogInformation("Kích hoạt quy tắc tự động hóa: {RuleName} cho vé {TicketNumber}", rule.Name, ticket.TicketNumber);

                await ExecuteActionsAsync(ticket, rule.GetActions(), rule.Name);
                anyExecuted = true;

                if (rule.StopProcessing)
                {
                    _logger.LogInformation("Quy tắc {RuleName} yêu cầu dừng xử lý các quy tắc tiếp theo.", rule.Name);
                    break;
                }
            }
        }

        return anyExecuted;
    }

    public async Task<int> ExecuteScheduledRulesAsync()
    {
        var rules = (await _ruleRepository.GetListAsync(r => r.IsActive && r.TriggerType == AutomationTriggerType.ScheduledTime))
            .OrderBy(r => r.ExecutionOrder)
            .ToList();

        if (rules.Count == 0)
        {
            return 0;
        }

        // Lấy các vé chưa đóng
        var ticketQuery = await _ticketRepository.GetQueryableAsync();
        var statusQuery = await _statusRepository.GetQueryableAsync();

        // Lấy danh sách ID các trạng thái đóng/kết thúc
        var closedStatusIds = await _asyncExecuter.ToListAsync(
            statusQuery.Where(s => s.IsFinal || s.StatusGroup == StatusGroup.Closed).Select(s => s.Id)
        );

        var activeTickets = await _asyncExecuter.ToListAsync(
            ticketQuery.Where(t => !closedStatusIds.Contains(t.StatusId))
        );

        int affectedCount = 0;
        var now = _clock.Now;

        foreach (var ticket in activeTickets)
        {
            bool ticketModified = false;
            foreach (var rule in rules)
            {
                var conditions = rule.GetConditions();
                if (EvaluateConditions(ticket, conditions, now, null))
                {
                    _logger.LogInformation("Scheduled Rule {RuleName} kích hoạt cho vé {TicketNumber}", rule.Name, ticket.TicketNumber);
                    await ExecuteActionsAsync(ticket, rule.GetActions(), rule.Name);
                    ticketModified = true;

                    if (rule.StopProcessing)
                    {
                        break;
                    }
                }
            }

            if (ticketModified)
            {
                await _ticketRepository.UpdateAsync(ticket, autoSave: true);
                affectedCount++;
            }
        }

        return affectedCount;
    }

    public async Task<bool> ApplyMacroAsync(Ticket ticket, Guid macroId)
    {
        var macro = await _macroRepository.FindAsync(macroId);
        if (macro == null || !macro.IsActive)
        {
            return false;
        }

        var actions = macro.GetActions();
        if (actions.Count == 0)
        {
            return false;
        }

        _logger.LogInformation("Áp dụng Macro: {MacroName} cho vé {TicketNumber}", macro.Name, ticket.TicketNumber);
        await ExecuteActionsAsync(ticket, actions, $"Macro: {macro.Name}");
        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        return true;
    }

    private bool EvaluateConditions(Ticket ticket, List<RuleCondition> conditions, DateTime now, string? contextComment)
    {
        if (conditions == null || conditions.Count == 0)
        {
            // Không có điều kiện nào -> luôn khớp
            return true;
        }

        foreach (var cond in conditions)
        {
            if (!EvaluateSingleCondition(ticket, cond, now, contextComment))
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateSingleCondition(Ticket ticket, RuleCondition cond, DateTime now, string? contextComment)
    {
        var val = cond.Value?.Trim() ?? string.Empty;

        switch (cond.Field)
        {
            case ConditionField.Status:
                return MatchString(ticket.StatusId.ToString(), cond.Operator, val);

            case ConditionField.Priority:
                return MatchString(ticket.PriorityId.ToString(), cond.Operator, val);

            case ConditionField.Category:
                return MatchString(ticket.CategoryId.ToString(), cond.Operator, val);

            case ConditionField.Department:
                return MatchString(ticket.DepartmentId?.ToString() ?? string.Empty, cond.Operator, val);

            case ConditionField.Assignee:
                return MatchString(ticket.AssigneeId?.ToString() ?? string.Empty, cond.Operator, val);

            case ConditionField.Source:
                return MatchString(ticket.SourceId.ToString(), cond.Operator, val);

            case ConditionField.Title:
                return MatchString(ticket.Title, cond.Operator, val);

            case ConditionField.Description:
                return MatchString(ticket.Description, cond.Operator, val);

            case ConditionField.Tags:
                return MatchString(ticket.Tags ?? string.Empty, cond.Operator, val);

            case ConditionField.IsUnassigned:
                bool isUnassigned = !ticket.AssigneeId.HasValue || ticket.AssigneeId == Guid.Empty;
                if (bool.TryParse(val, out var expectedUnassigned))
                {
                    return isUnassigned == expectedUnassigned;
                }
                return isUnassigned;

            case ConditionField.HoursSinceLastUpdate:
                var lastActivityTime = ticket.LastModificationTime ?? ticket.CreationTime;
                var hoursInactive = (now - lastActivityTime).TotalHours;
                if (double.TryParse(val, out var expectedHours))
                {
                    return MatchNumeric(hoursInactive, cond.Operator, expectedHours);
                }
                return false;

            case ConditionField.HoursSinceCreated:
                var hoursCreated = (now - ticket.CreationTime).TotalHours;
                if (double.TryParse(val, out var expHoursCreated))
                {
                    return MatchNumeric(hoursCreated, cond.Operator, expHoursCreated);
                }
                return false;

            default:
                return false;
        }
    }

    private static bool MatchString(string source, ConditionOperator op, string target)
    {
        switch (op)
        {
            case ConditionOperator.Equals:
                return string.Equals(source, target, StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.NotEquals:
                return !string.Equals(source, target, StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.Contains:
                return source.Contains(target, StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.NotContains:
                return !source.Contains(target, StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.IsEmpty:
                return string.IsNullOrWhiteSpace(source);

            case ConditionOperator.IsNotEmpty:
                return !string.IsNullOrWhiteSpace(source);

            default:
                return false;
        }
    }

    private static bool MatchNumeric(double source, ConditionOperator op, double target)
    {
        switch (op)
        {
            case ConditionOperator.GreaterThan:
                return source >= target;

            case ConditionOperator.LessThan:
                return source <= target;

            case ConditionOperator.Equals:
                return Math.Abs(source - target) < 0.01;

            case ConditionOperator.NotEquals:
                return Math.Abs(source - target) >= 0.01;

            default:
                return false;
        }
    }

    private async Task ExecuteActionsAsync(Ticket ticket, List<RuleAction> actions, string sourceName)
    {
        if (actions == null || actions.Count == 0)
        {
            return;
        }

        foreach (var action in actions)
        {
            try
            {
                switch (action.ActionType)
                {
                    case AutomationActionType.ChangeStatus:
                        if (Guid.TryParse(action.TargetValue, out var targetStatusId))
                        {
                            var newStatus = await _statusRepository.FindAsync(targetStatusId);
                            var oldStatus = await _statusRepository.FindAsync(ticket.StatusId);
                            if (newStatus != null && oldStatus != null && newStatus.Id != ticket.StatusId)
                            {
                                await _ticketManager.ChangeStatusAsync(ticket, oldStatus, newStatus);
                            }
                        }
                        break;

                    case AutomationActionType.ChangePriority:
                        if (Guid.TryParse(action.TargetValue, out var targetPriorityId))
                        {
                            var newPriority = await _priorityRepository.FindAsync(targetPriorityId);
                            if (newPriority != null && newPriority.Id != ticket.PriorityId)
                            {
                                var oldPriority = await _priorityRepository.FindAsync(ticket.PriorityId);
                                ticket.PriorityId = newPriority.Id;

                                var activity = new TicketActivity(
                                    GuidGenerator.Create(),
                                    ticket.Id,
                                    TicketActivityType.PriorityChanged,
                                    fieldName: "Priority",
                                    oldVal: oldPriority?.Name,
                                    newVal: newPriority.Name,
                                    description: $"Độ ưu tiên được tự động chuyển thành '{newPriority.Name}' bởi {sourceName}."
                                );
                                await _activityRepository.InsertAsync(activity);
                            }
                        }
                        break;

                    case AutomationActionType.AssignToUser:
                        if (Guid.TryParse(action.TargetValue, out var targetUserId))
                        {
                            var user = await _userRepository.FindAsync(targetUserId);
                            if (user != null && ticket.AssigneeId != user.Id)
                            {
                                await _ticketManager.AssignAsync(ticket, user.Id, user.UserName, null, null);
                            }
                        }
                        break;

                    case AutomationActionType.AssignToDepartment:
                        if (Guid.TryParse(action.TargetValue, out var targetDeptId))
                        {
                            var dept = await _departmentRepository.FindAsync(targetDeptId);
                            if (dept != null && ticket.DepartmentId != dept.Id)
                            {
                                await _ticketManager.AssignAsync(ticket, ticket.AssigneeId, null, dept.Id, dept.Name);
                            }
                        }
                        break;

                    case AutomationActionType.AddTags:
                        if (!string.IsNullOrWhiteSpace(action.TargetValue))
                        {
                            var tagToAdd = action.TargetValue.Trim();
                            var currentTags = (ticket.Tags ?? string.Empty)
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(t => t.Trim())
                                .ToList();

                            if (!currentTags.Contains(tagToAdd, StringComparer.OrdinalIgnoreCase))
                            {
                                currentTags.Add(tagToAdd);
                                ticket.Tags = string.Join(", ", currentTags);
                            }
                        }
                        break;

                    case AutomationActionType.AddComment:
                        if (!string.IsNullOrWhiteSpace(action.TargetValue))
                        {
                            bool isInternal = string.Equals(action.AdditionalValue, "true", StringComparison.OrdinalIgnoreCase);
                            var comment = new TicketComment(
                                GuidGenerator.Create(),
                                ticket.Id,
                                action.TargetValue.Trim(),
                                isInternal: isInternal,
                                authorName: "⚡ Hệ Thống Tự Động"
                            );
                            await _commentRepository.InsertAsync(comment);

                            var activityType = isInternal ? TicketActivityType.InternalNoteAdded : TicketActivityType.CommentAdded;
                            var activity = new TicketActivity(
                                GuidGenerator.Create(),
                                ticket.Id,
                                activityType,
                                description: $"Bình luận tự động được thêm bởi {sourceName}."
                            );
                            await _activityRepository.InsertAsync(activity);
                        }
                        break;

                    case AutomationActionType.SendDiscordAlert:
                        var priorityForAlert = await _priorityRepository.FindAsync(ticket.PriorityId);
                        await _discordNotificationService.SendTicketCreatedAsync(ticket, "Cảnh Báo Tự Động", priorityForAlert?.Name ?? "Urgent", isCritical: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thực thi hành động {ActionType} của {SourceName}", action.ActionType, sourceName);
            }
        }

        // Ghi vết thực thi tự động hóa vào dòng thời gian hoạt động
        var auditActivity = new TicketActivity(
            GuidGenerator.Create(),
            ticket.Id,
            TicketActivityType.AutomationExecuted,
            description: $"⚡ Tự động hóa: Quy tắc '{sourceName}' đã được thực thi thành công."
        );
        await _activityRepository.InsertAsync(auditActivity);
    }
}
