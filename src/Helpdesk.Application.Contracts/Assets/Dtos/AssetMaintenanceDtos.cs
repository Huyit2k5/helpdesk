using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Assets.Dtos;

public class AssetMaintenanceDto : FullAuditedEntityDto<Guid>
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public MaintenanceType MaintenanceType { get; set; }
    public string MaintenanceTypeName { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ServiceProvider { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? ReplacedParts { get; set; }
    public DateTime? PartsWarrantyExpiry { get; set; }
    public string? Notes { get; set; }
    public Guid? RelatedTicketId { get; set; }
    public string? RelatedTicketNumber { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
}

public class CreateAssetMaintenanceDto
{
    public Guid AssetId { get; set; }
    public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Repair;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ServiceProvider { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public decimal? EstimatedCost { get; set; }
    public Guid? RelatedTicketId { get; set; }
    public bool SetAssetUnderRepair { get; set; } = true;
}

public class CompleteAssetMaintenanceDto
{
    public decimal? ActualCost { get; set; }
    public DateTime? ActualCompletionDate { get; set; }
    public string? ReplacedParts { get; set; }
    public DateTime? PartsWarrantyExpiry { get; set; }
    public string? Notes { get; set; }
    public bool ReturnToStock { get; set; } = false;
    public DateTime? NextMaintenanceDate { get; set; }
}

public class GetAssetMaintenanceListInput : PagedAndSortedResultRequestDto
{
    public Guid? AssetId { get; set; }
    public MaintenanceType? MaintenanceType { get; set; }
    public MaintenanceStatus? Status { get; set; }
    public string? Filter { get; set; }
}

public class AssetTcoSummaryDto
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public decimal PurchaseCost { get; set; }
    public decimal TotalMaintenanceCost { get; set; }
    public decimal TotalCostOfOwnership { get; set; }
    public int MaintenanceCount { get; set; }
    public double RepairCostPercentage { get; set; } // (TotalMaintenanceCost / PurchaseCost) * 100
    public string Recommendation { get; set; } = "Kinh tế"; // "Kinh tế", "Cần theo dõi", "Khuyến nghị thanh lý"
    public string RecommendationColor { get; set; } = "success"; // "success", "warning", "danger"
    public DateTime? LastMaintenanceDate { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public bool IsDueForMaintenance { get; set; }
}

public class MaintenanceScheduleAlertDto
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetTypeName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? AssignedToUserName { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
    public int DaysOverdueOrRemaining { get; set; }
    public bool IsOverdue { get; set; }
}
