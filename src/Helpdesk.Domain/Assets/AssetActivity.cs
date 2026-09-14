using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Assets;

/// <summary>
/// Nhật ký hoạt động & vòng đời thiết bị (Asset Activity Log / Audit Trail)
/// </summary>
public class AssetActivity : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid AssetId { get; private set; }

    public AssetActivityType ActivityType { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public Guid? PerformedByUserId { get; private set; }

    public string? PerformedByUserName { get; private set; }

    public Guid? RelatedTicketId { get; private set; }

    protected AssetActivity()
    {
        // For EF Core
    }

    public AssetActivity(
        Guid id,
        Guid assetId,
        AssetActivityType activityType,
        string title,
        string? description = null,
        Guid? performedByUserId = null,
        string? performedByUserName = null,
        Guid? relatedTicketId = null)
        : base(id)
    {
        AssetId = assetId;
        ActivityType = activityType;
        Title = title;
        Description = description;
        PerformedByUserId = performedByUserId;
        PerformedByUserName = performedByUserName;
        RelatedTicketId = relatedTicketId;
    }
}
