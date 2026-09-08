using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Sla;

public class Holiday : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public DateTime Date { get; set; }

    public bool IsRecurring { get; set; }

    protected Holiday() { }

    public Holiday(
        Guid id,
        string name,
        DateTime date,
        bool isRecurring = false,
        Guid? tenantId = null) : base(id)
    {
        SetName(name);
        Date = date.Date;
        IsRecurring = isRecurring;
        TenantId = tenantId;
    }

    public void SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), SlaConsts.MaxHolidayNameLength);
    }
}
