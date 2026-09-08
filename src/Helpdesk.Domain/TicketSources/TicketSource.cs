using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.TicketSources;

/// <summary>
/// Nguồn tiếp nhận ticket: Email, Phone, Web Portal, Chat, Walk-in...
/// </summary>
public class TicketSource : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    public bool IsActive { get; set; } = true;

    protected TicketSource()
    {
    }

    public TicketSource(
        Guid id,
        string name,
        string code,
        bool isActive = true)
        : base(id)
    {
        SetName(name);
        SetCode(code);
        IsActive = isActive;
    }

    public TicketSource SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), TicketSourceConsts.MaxNameLength);
        return this;
    }

    public TicketSource SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), TicketSourceConsts.MaxCodeLength);
        return this;
    }
}
