using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Assets.Dtos;

public class AssetDto : FullAuditedEntityDto<Guid>
{
    public string AssetTag { get; set; } = null!;
    public string Name { get; set; } = null!;
    public AssetType AssetType { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;
    public AssetStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Location { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? AssignedToUserEmail { get; set; }
    public string? Department { get; set; }
    public DateTime? AssignedDate { get; set; }
    public string? Specifications { get; set; }
    public string? Notes { get; set; }
    public bool IsHandoverConfirmed { get; set; }
    public DateTime? HandoverConfirmedDate { get; set; }
    public string? HandoverNotes { get; set; }
    public int OpenTicketCount { get; set; }
}

public class AssetDetailDto : AssetDto
{
    public List<AssetActivityDto> Activities { get; set; } = new();
    public List<AssetTicketDto> Tickets { get; set; } = new();
}

public class AssetActivityDto : CreationAuditedEntityDto<Guid>
{
    public Guid AssetId { get; set; }
    public AssetActivityType ActivityType { get; set; }
    public string ActivityTypeName { get; set; } = string.Empty;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
    public Guid? RelatedTicketId { get; set; }
}

public class AssetTicketDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string PriorityName { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
}

public class GetAssetsInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public AssetType? AssetType { get; set; }
    public AssetStatus? Status { get; set; }
    public string? Department { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool? WarrantyExpiringSoon { get; set; }
}

public class CreateAssetDto
{
    [StringLength(AssetConsts.MaxAssetTagLength)]
    public string? AssetTag { get; set; }

    [Required]
    [StringLength(AssetConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    public AssetType AssetType { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.InStock;

    [StringLength(AssetConsts.MaxSerialNumberLength)]
    public string? SerialNumber { get; set; }

    [StringLength(AssetConsts.MaxModelLength)]
    public string? Model { get; set; }

    [StringLength(AssetConsts.MaxManufacturerLength)]
    public string? Manufacturer { get; set; }

    [StringLength(AssetConsts.MaxLocationLength)]
    public string? Location { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public decimal? PurchaseCost { get; set; }

    [StringLength(AssetConsts.MaxSpecsLength)]
    public string? Specifications { get; set; }

    [StringLength(AssetConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}

public class UpdateAssetDto
{
    [Required]
    [StringLength(AssetConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    public AssetType AssetType { get; set; }

    [StringLength(AssetConsts.MaxSerialNumberLength)]
    public string? SerialNumber { get; set; }

    [StringLength(AssetConsts.MaxModelLength)]
    public string? Model { get; set; }

    [StringLength(AssetConsts.MaxManufacturerLength)]
    public string? Manufacturer { get; set; }

    [StringLength(AssetConsts.MaxLocationLength)]
    public string? Location { get; set; }

    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public decimal? PurchaseCost { get; set; }

    [StringLength(AssetConsts.MaxSpecsLength)]
    public string? Specifications { get; set; }

    [StringLength(AssetConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}

public class AssignAssetDto
{
    public Guid? UserId { get; set; }

    [Required]
    [StringLength(AssetConsts.MaxAssigneeNameLength)]
    public string UserName { get; set; } = null!;

    [EmailAddress]
    [StringLength(AssetConsts.MaxAssigneeEmailLength)]
    public string? UserEmail { get; set; }

    [StringLength(AssetConsts.MaxDepartmentLength)]
    public string? Department { get; set; }

    [StringLength(AssetConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}

public class ReturnAssetDto
{
    [StringLength(AssetConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}

public class ChangeAssetStatusDto
{
    [Required]
    public AssetStatus Status { get; set; }

    public string? Reason { get; set; }
}

public class AssetTypeStockDto
{
    public AssetType AssetType { get; set; }
    public string AssetTypeName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int InStockCount { get; set; }
    public int AssignedCount { get; set; }
}

public class AssetKpiDto
{
    public int TotalAssets { get; set; }
    public int InStockCount { get; set; }
    public int AssignedCount { get; set; }
    public int UnderRepairCount { get; set; }
    public int WarrantyExpiringSoonCount { get; set; }
    public List<AssetTypeStockDto> StockByType { get; set; } = new();
}

public class ConfirmAssetHandoverDto
{
    [StringLength(AssetConsts.MaxNotesLength)]
    public string? Notes { get; set; }
}

public class AssetReceiptDto
{
    public Guid AssetId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = "handover"; // "handover" or "return"
    public string Title { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }

    // IT Handover Party
    public string GiverName { get; set; } = string.Empty;
    public string GiverRole { get; set; } = "Bộ phận Kỹ thuật & CNTT";
    public string GiverEmail { get; set; } = string.Empty;

    // Receiver Party
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverEmail { get; set; } = string.Empty;
    public string ReceiverDepartment { get; set; } = string.Empty;

    // Asset Info
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssetTypeName { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Specifications { get; set; }
    public string? Location { get; set; }
    public DateTime? AssignedDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public string Condition { get; set; } = "Hoạt động bình thường, nguyên vẹn";
    public string Accessories { get; set; } = "Bộ sạc cáp nguồn tiêu chuẩn, chuột máy tính, túi bảo vệ";
    public string? Notes { get; set; }

    public bool IsConfirmed { get; set; }
    public DateTime? ConfirmedDate { get; set; }
}
