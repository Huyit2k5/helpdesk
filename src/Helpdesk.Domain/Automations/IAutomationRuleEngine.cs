using System;
using System.Threading.Tasks;
using Helpdesk.Tickets;

namespace Helpdesk.Automations;

public interface IAutomationRuleEngine
{
    /// <summary>
    /// Thực thi các quy tắc tự động hóa theo sự kiện (OnTicketCreated, OnTicketUpdated, OnCommentAdded).
    /// </summary>
    Task<bool> ExecuteTriggersAsync(Ticket ticket, AutomationTriggerType triggerType, string? contextComment = null);

    /// <summary>
    /// Quét định kỳ tất cả các sự vụ đang mở để thực thi các quy tắc theo thời gian (ScheduledTime).
    /// </summary>
    Task<int> ExecuteScheduledRulesAsync();

    /// <summary>
    /// Áp dụng mẫu thao tác nhanh 1-Click (Macro) vào sự vụ.
    /// </summary>
    Task<bool> ApplyMacroAsync(Ticket ticket, Guid macroId);
}
