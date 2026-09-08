using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Departments;

/// <summary>
/// Phòng ban xử lý ticket.
/// </summary>
public class Department : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    /// <summary>
    /// ID người quản lý phòng ban (FK → IdentityUser).
    /// </summary>
    public Guid? ManagerId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    protected Department()
    {
    }

    public Department(
        Guid id,
        string name,
        string code,
        Guid? managerId = null,
        string? description = null,
        bool isActive = true)
        : base(id)
    {
        SetName(name);
        SetCode(code);
        ManagerId = managerId;
        Description = description;
        IsActive = isActive;
    }

    public Department SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), DepartmentConsts.MaxNameLength);
        return this;
    }

    public Department SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), DepartmentConsts.MaxCodeLength);
        return this;
    }
}
