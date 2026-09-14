using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.Dashboard.Dtos;
using Helpdesk.Permissions;
using Helpdesk.Tickets;
using Helpdesk.TicketStatuses;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Helpdesk.Dashboard;

[Authorize(HelpdeskPermissions.Dashboard.Default)]
public class DashboardAppService : ApplicationService, IDashboardAppService
{
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketActivity, Guid> _activityRepository;
    private readonly IRepository<TicketStatus, Guid> _statusRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public DashboardAppService(
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketActivity, Guid> activityRepository,
        IRepository<TicketStatus, Guid> statusRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<IdentityUser, Guid> userRepository)
    {
        _ticketRepository = ticketRepository;
        _activityRepository = activityRepository;
        _statusRepository = statusRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(GetDashboardInput input)
    {
        var now = Clock.Now;
        var today = now.Date;

        var ticketQuery = await _ticketRepository.GetQueryableAsync();
        var statusQuery = await _statusRepository.GetQueryableAsync();

        // Apply department filter
        if (input.DepartmentId.HasValue && input.DepartmentId.Value != Guid.Empty)
        {
            ticketQuery = ticketQuery.Where(t => t.DepartmentId == input.DepartmentId.Value);
        }

        // Apply date filters
        if (input.StartDate.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CreationTime >= input.StartDate.Value);
        }
        if (input.EndDate.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CreationTime <= input.EndDate.Value);
        }

        var allTickets = await AsyncExecuter.ToListAsync(ticketQuery);
        var allStatuses = await AsyncExecuter.ToListAsync(statusQuery);

        var statusGroupMap = allStatuses.ToDictionary(s => s.Id, s => s.StatusGroup);

        // === Ticket Overview ===
        int totalTickets = allTickets.Count;
        int openTickets = allTickets.Count(t => statusGroupMap.GetValueOrDefault(t.StatusId) == StatusGroup.Open);
        int inProgressTickets = allTickets.Count(t => statusGroupMap.GetValueOrDefault(t.StatusId) == StatusGroup.InProgress);
        int closedTickets = allTickets.Count(t => statusGroupMap.GetValueOrDefault(t.StatusId) == StatusGroup.Closed);
        int newTicketsToday = allTickets.Count(t => t.CreationTime.Date == today);
        int resolvedToday = allTickets.Count(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == today);

        // === Overdue ===
        int overdueTicketCount = allTickets.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value < now &&
            statusGroupMap.GetValueOrDefault(t.StatusId) != StatusGroup.Closed);

        // === Cá nhân (ticket của người dùng đang đăng nhập) ===
        int myOpenTicketCount = CurrentUser.Id.HasValue
            ? allTickets.Count(t => t.AssigneeId == CurrentUser.Id.Value &&
                statusGroupMap.GetValueOrDefault(t.StatusId) != StatusGroup.Closed)
            : 0;
        int myOverdueTicketCount = CurrentUser.Id.HasValue
            ? allTickets.Count(t => t.AssigneeId == CurrentUser.Id.Value &&
                t.DueDate.HasValue && t.DueDate.Value < now &&
                statusGroupMap.GetValueOrDefault(t.StatusId) != StatusGroup.Closed)
            : 0;

        // === SLA Compliance ===
        var slaTickets = allTickets.Where(t => t.SlaPolicyId.HasValue).ToList();
        int totalSla = slaTickets.Count;
        int firstResponseBreached = slaTickets.Count(t => t.IsFirstResponseBreached);
        int resolutionBreached = slaTickets.Count(t => t.IsResolutionBreached);

        double firstResponseRate = totalSla > 0 ? Math.Round((double)(totalSla - firstResponseBreached) / totalSla * 100, 1) : 100;
        double resolutionRate = totalSla > 0 ? Math.Round((double)(totalSla - resolutionBreached) / totalSla * 100, 1) : 100;

        // === CSAT Metrics ===
        // null khi chưa có lượt khảo sát nào - tránh hiển thị "hài lòng tuyệt đối" giả khi thực ra chưa ai đánh giá.
        var ratedTickets = allTickets.Where(t => t.CsatRating.HasValue).ToList();
        int totalRated = ratedTickets.Count;
        double? avgCsat = totalRated > 0 ? Math.Round(ratedTickets.Average(t => t.CsatRating!.Value), 1) : null;
        int satisfiedCount = ratedTickets.Count(t => t.CsatRating!.Value >= 4);
        double? satisfactionRate = totalRated > 0 ? Math.Round((double)satisfiedCount / totalRated * 100, 1) : null;

        // === Ticket Trend ===
        var trendDays = input.TrendDays > 0 ? input.TrendDays : 30;
        var trendStartDate = today.AddDays(-trendDays + 1);
        var trend = BuildTicketTrend(allTickets, trendStartDate, today);

        // === Category Distribution ===
        var categoryDistribution = await BuildCategoryDistributionAsync(allTickets);

        // === Agent Performance ===
        var agentPerformance = await BuildAgentPerformanceAsync(allTickets);

        // === Recent Activities ===
        var recentActivities = await BuildRecentActivitiesAsync(input.DepartmentId);

        return new DashboardStatsDto
        {
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            InProgressTickets = inProgressTickets,
            ClosedTickets = closedTickets,
            NewTicketsToday = newTicketsToday,
            ResolvedToday = resolvedToday,
            FirstResponseComplianceRate = firstResponseRate,
            ResolutionComplianceRate = resolutionRate,
            SlaBreachedCount = firstResponseBreached + resolutionBreached,
            AvgCsatRating = avgCsat,
            TotalRatedTickets = totalRated,
            CsatSatisfactionRate = satisfactionRate,
            OverdueTicketCount = overdueTicketCount,
            MyOpenTicketCount = myOpenTicketCount,
            MyOverdueTicketCount = myOverdueTicketCount,
            TicketTrend = trend,
            CategoryDistribution = categoryDistribution,
            AgentPerformance = agentPerformance,
            RecentActivities = recentActivities
        };
    }

    public async Task<List<TicketTrendItemDto>> GetTicketTrendAsync(GetDashboardInput input)
    {
        var now = Clock.Now;
        var today = now.Date;
        var trendDays = input.TrendDays > 0 ? input.TrendDays : 30;
        var trendStartDate = today.AddDays(-trendDays + 1);

        var ticketQuery = await _ticketRepository.GetQueryableAsync();

        if (input.DepartmentId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.DepartmentId == input.DepartmentId.Value);
        }

        ticketQuery = ticketQuery.Where(t => t.CreationTime >= trendStartDate);

        var tickets = await AsyncExecuter.ToListAsync(ticketQuery);
        return BuildTicketTrend(tickets, trendStartDate, today);
    }

    public async Task<List<AgentPerformanceDto>> GetAgentPerformanceAsync(GetDashboardInput input)
    {
        var ticketQuery = await _ticketRepository.GetQueryableAsync();

        if (input.DepartmentId.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.DepartmentId == input.DepartmentId.Value);
        }
        if (input.StartDate.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CreationTime >= input.StartDate.Value);
        }
        if (input.EndDate.HasValue)
        {
            ticketQuery = ticketQuery.Where(t => t.CreationTime <= input.EndDate.Value);
        }

        var tickets = await AsyncExecuter.ToListAsync(ticketQuery);
        return await BuildAgentPerformanceAsync(tickets);
    }

    // ===================== Private Helpers =====================

    private static List<TicketTrendItemDto> BuildTicketTrend(List<Ticket> tickets, DateTime startDate, DateTime endDate)
    {
        var result = new List<TicketTrendItemDto>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            result.Add(new TicketTrendItemDto
            {
                Date = date,
                CreatedCount = tickets.Count(t => t.CreationTime.Date == date),
                ResolvedCount = tickets.Count(t => t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == date),
                ClosedCount = tickets.Count(t => t.ClosedAt.HasValue && t.ClosedAt.Value.Date == date)
            });
        }

        return result;
    }

    private async Task<List<CategoryDistributionDto>> BuildCategoryDistributionAsync(List<Ticket> tickets)
    {
        if (tickets.Count == 0) return new List<CategoryDistributionDto>();

        var categoryIds = tickets.Select(t => t.CategoryId).Distinct().ToList();
        var categories = await _categoryRepository.GetListAsync(c => categoryIds.Contains(c.Id));
        var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

        var groups = tickets
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryDistributionDto
            {
                CategoryId = g.Key,
                CategoryName = categoryDict.GetValueOrDefault(g.Key, "Unknown"),
                TicketCount = g.Count(),
                Percentage = Math.Round((double)g.Count() / tickets.Count * 100, 1)
            })
            .OrderByDescending(x => x.TicketCount)
            .ToList();

        return groups;
    }

    private async Task<List<AgentPerformanceDto>> BuildAgentPerformanceAsync(List<Ticket> tickets)
    {
        var assignedTickets = tickets.Where(t => t.AssigneeId.HasValue).ToList();
        if (assignedTickets.Count == 0) return new List<AgentPerformanceDto>();

        var agentIds = assignedTickets.Select(t => t.AssigneeId!.Value).Distinct().ToList();
        var users = await _userRepository.GetListAsync(u => agentIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id, u => u.UserName ?? u.Email ?? "N/A");

        var result = agentIds.Select(agentId =>
        {
            var agentTickets = assignedTickets.Where(t => t.AssigneeId == agentId).ToList();
            var resolved = agentTickets.Where(t => t.ResolvedAt.HasValue).ToList();
            var slaTickets = agentTickets.Where(t => t.SlaPolicyId.HasValue).ToList();
            var slaBreached = slaTickets.Count(t => t.IsFirstResponseBreached || t.IsResolutionBreached);

            double avgResolution = 0;
            if (resolved.Count > 0)
            {
                avgResolution = resolved
                    .Where(t => t.ResolvedAt.HasValue)
                    .Average(t => (t.ResolvedAt!.Value - t.CreationTime).TotalMinutes);
            }

            double slaRate = slaTickets.Count > 0
                ? Math.Round((double)(slaTickets.Count - slaBreached) / slaTickets.Count * 100, 1)
                : 100;

            var agentRated = agentTickets.Where(t => t.CsatRating.HasValue).ToList();
            double? agentAvgCsat = agentRated.Count > 0 ? Math.Round(agentRated.Average(t => t.CsatRating!.Value), 1) : null;

            return new AgentPerformanceDto
            {
                UserId = agentId,
                UserName = userDict.GetValueOrDefault(agentId, "Unknown"),
                AssignedCount = agentTickets.Count,
                ResolvedCount = resolved.Count,
                AvgResolutionMinutes = Math.Round(avgResolution, 0),
                SlaComplianceRate = slaRate,
                AvgCsatRating = agentAvgCsat,
                RatedTicketsCount = agentRated.Count
            };
        })
        .OrderByDescending(x => x.ResolvedCount)
        .Take(10)
        .ToList();

        return result;
    }

    private async Task<List<RecentActivityDto>> BuildRecentActivitiesAsync(Guid? departmentId)
    {
        var activityQuery = await _activityRepository.GetQueryableAsync();
        var ticketQuery = await _ticketRepository.GetQueryableAsync();

        if (departmentId.HasValue)
        {
            var ticketIds = await AsyncExecuter.ToListAsync(
                ticketQuery.Where(t => t.DepartmentId == departmentId.Value).Select(t => t.Id));
            activityQuery = activityQuery.Where(a => ticketIds.Contains(a.TicketId));
        }

        var recentActivities = await AsyncExecuter.ToListAsync(
            activityQuery.OrderByDescending(a => a.CreationTime).Take(20));

        var activityTicketIds = recentActivities.Select(a => a.TicketId).Distinct().ToList();
        var tickets = await _ticketRepository.GetListAsync(t => activityTicketIds.Contains(t.Id));
        var ticketDict = tickets.ToDictionary(t => t.Id, t => new { t.TicketNumber, t.Title });

        var creatorIds = recentActivities
            .Where(a => a.CreatorId.HasValue)
            .Select(a => a.CreatorId!.Value)
            .Distinct()
            .ToList();
        var users = await _userRepository.GetListAsync(u => creatorIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id, u => u.UserName ?? u.Email ?? "N/A");

        return recentActivities.Select(a =>
        {
            var ticketInfo = ticketDict.GetValueOrDefault(a.TicketId);
            return new RecentActivityDto
            {
                TicketId = a.TicketId,
                TicketNumber = ticketInfo?.TicketNumber ?? string.Empty,
                TicketTitle = ticketInfo?.Title ?? string.Empty,
                ActivityType = a.ActivityType.ToString(),
                Description = a.Description,
                CreatorName = a.CreatorId.HasValue ? userDict.GetValueOrDefault(a.CreatorId.Value) : null,
                CreationTime = a.CreationTime
            };
        }).ToList();
    }
}
