using System;
using System.Collections.Generic;

namespace Helpdesk.Dashboard.Dtos;

public class DashboardStatsDto
{
    // Ticket Overview
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int NewTicketsToday { get; set; }
    public int ResolvedToday { get; set; }

    // SLA Compliance
    public double FirstResponseComplianceRate { get; set; }
    public double ResolutionComplianceRate { get; set; }
    public int SlaBreachedCount { get; set; }

    // CSAT Metrics
    public double AvgCsatRating { get; set; }
    public int TotalRatedTickets { get; set; }
    public double CsatSatisfactionRate { get; set; }

    // Overdue
    public int OverdueTicketCount { get; set; }

    // Trend Data
    public List<TicketTrendItemDto> TicketTrend { get; set; } = new();

    // Category Distribution
    public List<CategoryDistributionDto> CategoryDistribution { get; set; } = new();

    // Agent Performance (top 10)
    public List<AgentPerformanceDto> AgentPerformance { get; set; } = new();

    // Recent Activities (last 20)
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class TicketTrendItemDto
{
    public DateTime Date { get; set; }
    public int CreatedCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
}

public class CategoryDistributionDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public double Percentage { get; set; }
}

public class AgentPerformanceDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int ResolvedCount { get; set; }
    public double AvgResolutionMinutes { get; set; }
    public double SlaComplianceRate { get; set; }
    public double AvgCsatRating { get; set; }
    public int RatedTicketsCount { get; set; }
}

public class RecentActivityDto
{
    public Guid TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }
}
