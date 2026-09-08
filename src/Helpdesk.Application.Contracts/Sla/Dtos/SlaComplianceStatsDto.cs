using System;

namespace Helpdesk.Sla.Dtos;

public class SlaComplianceStatsDto
{
    public int TotalTicketsWithSla { get; set; }
    public int FirstResponseMetCount { get; set; }
    public int FirstResponseBreachedCount { get; set; }
    public double FirstResponseComplianceRate => TotalTicketsWithSla > 0 
        ? Math.Round((double)FirstResponseMetCount / TotalTicketsWithSla * 100, 1) 
        : 100.0;

    public int ResolutionMetCount { get; set; }
    public int ResolutionBreachedCount { get; set; }
    public double ResolutionComplianceRate => TotalTicketsWithSla > 0 
        ? Math.Round((double)ResolutionMetCount / TotalTicketsWithSla * 100, 1) 
        : 100.0;
}
