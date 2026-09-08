using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Categories;

/// <summary>
/// Danh mục phân loại ticket, hỗ trợ cây phân cấp qua ParentId.
/// </summary>
public class Category : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; private set; } = null!;

    public string Code { get; private set; } = null!;

    /// <summary>
    /// ID danh mục cha, null nếu là root.
    /// </summary>
    public Guid? ParentId { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int Order { get; set; }

    protected Category()
    {
        // For EF Core
    }

    public Category(
        Guid id,
        string name,
        string code,
        Guid? parentId = null,
        string? description = null,
        bool isActive = true,
        int order = 0)
        : base(id)
    {
        SetName(name);
        SetCode(code);
        ParentId = parentId;
        Description = description;
        IsActive = isActive;
        Order = order;
    }

    public Category SetName(string name)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), CategoryConsts.MaxNameLength);
        return this;
    }

    public Category SetCode(string code)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), CategoryConsts.MaxCodeLength);
        return this;
    }
}
