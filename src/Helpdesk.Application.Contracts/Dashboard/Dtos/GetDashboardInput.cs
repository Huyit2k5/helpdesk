using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Dashboard.Dtos;

public class GetDashboardInput
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public int TrendDays { get; set; } = 30;
}
