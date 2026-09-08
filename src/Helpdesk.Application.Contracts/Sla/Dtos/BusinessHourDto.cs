using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Sla.Dtos;

public class BusinessHourDto : EntityDto<Guid>
{
    public DayOfWeek DayOfWeek { get; set; }
    public string DayOfWeekName => DayOfWeek.ToString();
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public bool IsWorkingDay { get; set; }
}

public class UpdateBusinessHourDto
{
    public Guid Id { get; set; }
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public bool IsWorkingDay { get; set; }
}
