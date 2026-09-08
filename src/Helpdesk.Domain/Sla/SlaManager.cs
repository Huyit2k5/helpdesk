using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Priorities;
using Helpdesk.Tickets;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace Helpdesk.Sla;

public class SlaManager : DomainService
{
    private readonly IRepository<SlaPolicy, Guid> _policyRepository;
    private readonly IRepository<BusinessHour, Guid> _businessHourRepository;
    private readonly IRepository<Holiday, Guid> _holidayRepository;
    private readonly IRepository<Priority, Guid> _priorityRepository;
    private readonly IRepository<SlaBreachLog, Guid> _breachLogRepository;
    private readonly IGuidGenerator _guidGenerator;

    public SlaManager(
        IRepository<SlaPolicy, Guid> policyRepository,
        IRepository<BusinessHour, Guid> businessHourRepository,
        IRepository<Holiday, Guid> holidayRepository,
        IRepository<Priority, Guid> priorityRepository,
        IRepository<SlaBreachLog, Guid> breachLogRepository,
        IGuidGenerator guidGenerator)
    {
        _policyRepository = policyRepository;
        _businessHourRepository = businessHourRepository;
        _holidayRepository = holidayRepository;
        _priorityRepository = priorityRepository;
        _breachLogRepository = breachLogRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task CalculateSlaDatesAsync(Ticket ticket)
    {
        var policies = await _policyRepository.GetListAsync(p => p.IsActive);
        var policy = policies.FirstOrDefault(p => p.IsDefault) ?? policies.FirstOrDefault();

        int responseMinutes = 240; // Default fallback: 4 hours
        int resolutionMinutes = 1440; // Default fallback: 24 hours

        Guid? matchedRuleId = null;

        if (policy != null)
        {
            ticket.SlaPolicyId = policy.Id;

            // Priority + Category rule
            var rule = policy.Rules.FirstOrDefault(r => r.PriorityId == ticket.PriorityId && r.CategoryId == ticket.CategoryId)
                       ?? policy.Rules.FirstOrDefault(r => r.PriorityId == ticket.PriorityId && r.CategoryId == null);

            if (rule != null)
            {
                matchedRuleId = rule.Id;
                responseMinutes = rule.ResponseTimeMinutes > 0 ? rule.ResponseTimeMinutes : responseMinutes;
                resolutionMinutes = rule.ResolutionTimeMinutes > 0 ? rule.ResolutionTimeMinutes : resolutionMinutes;
            }
        }

        // Priority SLA hours fallback
        var priority = await _priorityRepository.FindAsync(ticket.PriorityId);
        if (priority != null && matchedRuleId == null)
        {
            if (priority.SlaResponseHours > 0)
            {
                responseMinutes = priority.SlaResponseHours * 60;
            }

            if (priority.SlaResolutionHours > 0)
            {
                resolutionMinutes = priority.SlaResolutionHours * 60;
            }
        }

        var businessHours = await _businessHourRepository.GetListAsync();
        var holidays = await _holidayRepository.GetListAsync();

        var startTime = ticket.CreationTime != default ? ticket.CreationTime : DateTime.UtcNow;

        ticket.FirstResponseDueDate = CalculateTargetTime(startTime, responseMinutes, businessHours, holidays);
        ticket.DueDate = CalculateTargetTime(startTime, resolutionMinutes, businessHours, holidays);
    }

    public DateTime CalculateTargetTime(
        DateTime startUtc,
        int totalMinutes,
        List<BusinessHour> businessHours,
        List<Holiday> holidays)
    {
        if (businessHours == null || businessHours.Count == 0 || !businessHours.Any(b => b.IsWorkDay))
        {
            return startUtc.AddMinutes(totalMinutes);
        }

        var workDaysLookup = businessHours.ToDictionary(b => b.DayOfWeek);
        var current = startUtc;
        var remainingMinutes = totalMinutes;

        // Limit iteration up to 30 days to prevent any infinite loop
        int maxDays = 60;
        int daysChecked = 0;

        while (remainingMinutes > 0 && daysChecked < maxDays)
        {
            bool isHoliday = holidays.Any(h =>
                (h.IsRecurring && h.Date.Month == current.Month && h.Date.Day == current.Day) ||
                (!h.IsRecurring && h.Date.Date == current.Date));

            if (!isHoliday && workDaysLookup.TryGetValue(current.DayOfWeek, out var bh) && bh.IsWorkDay)
            {
                var workStart = current.Date.Add(bh.StartTime);
                var workEnd = current.Date.Add(bh.EndTime);

                if (current < workStart)
                {
                    current = workStart;
                }

                if (current < workEnd)
                {
                    var availableMins = (int)(workEnd - current).TotalMinutes;
                    if (availableMins >= remainingMinutes)
                    {
                        current = current.AddMinutes(remainingMinutes);
                        remainingMinutes = 0;
                        break;
                    }
                    else
                    {
                        remainingMinutes -= availableMins;
                        current = workEnd;
                    }
                }
            }

            // Move to start of next day
            current = current.Date.AddDays(1);
            daysChecked++;
        }

        return remainingMinutes == 0 ? current : startUtc.AddMinutes(totalMinutes);
    }

    public async Task OnCommentAddedAsync(Ticket ticket, bool isInternal)
    {
        if (!isInternal && !ticket.FirstRespondedAt.HasValue)
        {
            ticket.FirstRespondedAt = DateTime.UtcNow;

            if (ticket.FirstResponseDueDate.HasValue && ticket.FirstRespondedAt.Value > ticket.FirstResponseDueDate.Value)
            {
                ticket.IsFirstResponseBreached = true;

                var elapsed = (int)(ticket.FirstRespondedAt.Value - ticket.CreationTime).TotalMinutes;
                var breachLog = new SlaBreachLog(
                    _guidGenerator.Create(),
                    ticket.Id,
                    SlaBreachType.Response,
                    ticket.FirstResponseDueDate.Value,
                    DateTime.UtcNow,
                    resolvedOrRespondedAt: ticket.FirstRespondedAt,
                    elapsedMinutes: elapsed,
                    description: $"Vi phạm SLA phản hồi lần đầu. Hạn chót: {ticket.FirstResponseDueDate:dd/MM/yyyy HH:mm}, Phản hồi lúc: {ticket.FirstRespondedAt:dd/MM/yyyy HH:mm} (Trễ {elapsed} phút)",
                    tenantId: ticket.TenantId);

                await _breachLogRepository.InsertAsync(breachLog);
            }
        }
    }

    public async Task OnTicketResolvedAsync(Ticket ticket)
    {
        if (!ticket.ResolvedAt.HasValue)
        {
            ticket.ResolvedAt = DateTime.UtcNow;
        }

        if (ticket.DueDate.HasValue && ticket.ResolvedAt.Value > ticket.DueDate.Value)
        {
            ticket.IsResolutionBreached = true;

            var elapsed = (int)(ticket.ResolvedAt.Value - ticket.CreationTime).TotalMinutes;
            var breachLog = new SlaBreachLog(
                _guidGenerator.Create(),
                ticket.Id,
                SlaBreachType.Resolution,
                ticket.DueDate.Value,
                DateTime.UtcNow,
                resolvedOrRespondedAt: ticket.ResolvedAt,
                elapsedMinutes: elapsed,
                description: $"Vi phạm SLA giải quyết sự vụ. Hạn chót: {ticket.DueDate:dd/MM/yyyy HH:mm}, Hoàn tất lúc: {ticket.ResolvedAt:dd/MM/yyyy HH:mm} (Trễ {elapsed} phút)",
                tenantId: ticket.TenantId);

            await _breachLogRepository.InsertAsync(breachLog);
        }
    }

    public async Task CheckTicketBreachesAsync(Ticket ticket)
    {
        var now = DateTime.UtcNow;

        // Check response SLA breach
        if (!ticket.FirstRespondedAt.HasValue && ticket.FirstResponseDueDate.HasValue && now > ticket.FirstResponseDueDate.Value && !ticket.IsFirstResponseBreached)
        {
            ticket.IsFirstResponseBreached = true;
            var elapsed = (int)(now - ticket.CreationTime).TotalMinutes;

            var breachLog = new SlaBreachLog(
                _guidGenerator.Create(),
                ticket.Id,
                SlaBreachType.Response,
                ticket.FirstResponseDueDate.Value,
                now,
                elapsedMinutes: elapsed,
                description: $"Sự vụ chưa được phản hồi lần đầu và đã vượt quá hạn SLA: {ticket.FirstResponseDueDate:dd/MM/yyyy HH:mm}",
                tenantId: ticket.TenantId);

            await _breachLogRepository.InsertAsync(breachLog);
        }

        // Check resolution SLA breach
        if (!ticket.ResolvedAt.HasValue && ticket.DueDate.HasValue && now > ticket.DueDate.Value && !ticket.IsResolutionBreached)
        {
            ticket.IsResolutionBreached = true;
            var elapsed = (int)(now - ticket.CreationTime).TotalMinutes;

            var breachLog = new SlaBreachLog(
                _guidGenerator.Create(),
                ticket.Id,
                SlaBreachType.Resolution,
                ticket.DueDate.Value,
                now,
                elapsedMinutes: elapsed,
                description: $"Sự vụ chưa được xử lý xong và đã vượt quá hạn giải quyết SLA: {ticket.DueDate:dd/MM/yyyy HH:mm}",
                tenantId: ticket.TenantId);

            await _breachLogRepository.InsertAsync(breachLog);
        }
    }
}
