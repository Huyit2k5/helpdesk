using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Assets;

/// <summary>
/// Hồ sơ bảo trì, sửa chữa & nâng cấp thiết bị (Asset Maintenance & Repair Record)
/// </summary>
public class AssetMaintenance : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid AssetId { get; private set; }

    public MaintenanceType MaintenanceType { get; private set; }

    public MaintenanceStatus Status { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? ServiceProvider { get; private set; }

    public string? TrackingNumber { get; private set; }

    public DateTime? StartDate { get; private set; }

    public DateTime? ExpectedCompletionDate { get; private set; }

    public DateTime? ActualCompletionDate { get; private set; }

    public decimal? EstimatedCost { get; private set; }

    public decimal? ActualCost { get; private set; }

    public string? ReplacedParts { get; private set; }

    public DateTime? PartsWarrantyExpiry { get; private set; }

    public string? Notes { get; private set; }

    public Guid? RelatedTicketId { get; private set; }

    public Guid? PerformedByUserId { get; private set; }

    public string? PerformedByUserName { get; private set; }

    protected AssetMaintenance()
    {
        // For EF Core
    }

    public AssetMaintenance(
        Guid id,
        Guid assetId,
        MaintenanceType maintenanceType,
        string title,
        string? description = null,
        string? serviceProvider = null,
        string? trackingNumber = null,
        DateTime? startDate = null,
        DateTime? expectedCompletionDate = null,
        decimal? estimatedCost = null,
        Guid? relatedTicketId = null,
        Guid? performedByUserId = null,
        string? performedByUserName = null)
        : base(id)
    {
        AssetId = assetId;
        MaintenanceType = maintenanceType;
        Status = MaintenanceStatus.InProgress;
        Title = title;
        Description = description;
        ServiceProvider = serviceProvider;
        TrackingNumber = trackingNumber;
        StartDate = startDate ?? DateTime.Now;
        ExpectedCompletionDate = expectedCompletionDate;
        EstimatedCost = estimatedCost;
        RelatedTicketId = relatedTicketId;
        PerformedByUserId = performedByUserId;
        PerformedByUserName = performedByUserName;
    }

    public void Complete(
        decimal? actualCost,
        DateTime? actualCompletionDate = null,
        string? replacedParts = null,
        DateTime? partsWarrantyExpiry = null,
        string? notes = null)
    {
        Status = MaintenanceStatus.Completed;
        ActualCost = actualCost;
        ActualCompletionDate = actualCompletionDate ?? DateTime.Now;
        ReplacedParts = replacedParts;
        PartsWarrantyExpiry = partsWarrantyExpiry;
        if (!string.IsNullOrEmpty(notes))
        {
            Notes = string.IsNullOrEmpty(Notes) ? notes : $"{Notes}\n{notes}";
        }
    }

    public void Cancel(string? cancelReason = null)
    {
        Status = MaintenanceStatus.Cancelled;
        if (!string.IsNullOrEmpty(cancelReason))
        {
            Notes = string.IsNullOrEmpty(Notes) ? $"[ĐÃ HỦY]: {cancelReason}" : $"{Notes}\n[ĐÃ HỦY]: {cancelReason}";
        }
    }
}
