using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Sla;

public class BusinessHour : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public bool IsWorkDay { get; set; }

    protected BusinessHour() { }

    public BusinessHour(
        Guid id,
        DayOfWeek dayOfWeek,
        TimeSpan startTime,
        TimeSpan endTime,
        bool isWorkDay = true,
        Guid? tenantId = null) : base(id)
    {
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsWorkDay = isWorkDay;
        TenantId = tenantId;
    }
}
