using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Assets.Dtos;

public class AssetAuditSessionDto : FullAuditedEntityDto<Guid>
{
    public string Title { get; set; } = string.Empty;

    public string AuditCode { get; set; } = string.Empty;

    public string? ScopeDepartment { get; set; }

    public string? ScopeLocation { get; set; }

    public AssetAuditStatus Status { get; set; }

    public string StatusName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public int TotalExpectedCount { get; set; }

    public int ScannedCount { get; set; }

    public int MatchedCount { get; set; }

    public int DisplacedCount { get; set; }

    public int MissingCount { get; set; }

    public double ProgressPercentage { get; set; }

    public string? Notes { get; set; }

    public string? CreatorName { get; set; }

    public List<AssetAuditItemDto> Items { get; set; } = new();
}

public class CreateAssetAuditSessionDto
{
    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? ScopeDepartment { get; set; }

    [MaxLength(128)]
    public string? ScopeLocation { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class GetAssetAuditListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public AssetAuditStatus? Status { get; set; }

    public string? Department { get; set; }
}

public class AssetAuditItemDto : CreationAuditedEntityDto<Guid>
{
    public Guid AuditSessionId { get; set; }

    public Guid AssetId { get; set; }

    public string AssetTag { get; set; } = string.Empty;

    public string AssetName { get; set; } = string.Empty;

    public string AssetTypeName { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? Model { get; set; }

    public string? ExpectedLocation { get; set; }

    public Guid? ExpectedAssignedToUserId { get; set; }

    public string? ExpectedAssignedToUserName { get; set; }

    public string? ScannedLocation { get; set; }

    public Guid? ScannedAssignedToUserId { get; set; }

    public string? ScannedAssignedToUserName { get; set; }

    public AuditItemResult ResultStatus { get; set; }

    public string ResultStatusName { get; set; } = string.Empty;

    public DateTime? ScannedTime { get; set; }

    public Guid? ScannedByUserId { get; set; }

    public string? ScannedByUserName { get; set; }

    public string? Notes { get; set; }

    public bool IsReconciled { get; set; }

    public DateTime? ReconciledTime { get; set; }
}

public class ScanAuditItemInput
{
    [Required]
    public string Code { get; set; } = string.Empty; // Tag, Serial, QR URL or Id

    public string? CurrentLocation { get; set; }

    public Guid? CurrentAssignedUserId { get; set; }

    public string? CurrentAssignedUserName { get; set; }

    public string? Notes { get; set; }
}

public class ScanResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public AssetAuditItemDto? Item { get; set; }

    public AuditItemResult ResultStatus { get; set; }

    public bool IsDisplaced { get; set; }

    public string? DisplacedReason { get; set; }

    public AuditSessionCountsDto UpdatedCounts { get; set; } = new();
}

public class AuditSessionCountsDto
{
    public int TotalExpectedCount { get; set; }

    public int ScannedCount { get; set; }

    public int MatchedCount { get; set; }

    public int DisplacedCount { get; set; }

    public int MissingCount { get; set; }

    public double ProgressPercentage { get; set; }
}

public class ReconcileAuditItemsInput
{
    public List<Guid>? ItemIds { get; set; } // Null/empty means reconcile all Displaced items in session
}

public class AuditReportDto
{
    public AssetAuditSessionDto Session { get; set; } = new();

    public DateTime GeneratedDate { get; set; } = DateTime.Now;

    public string AuditorName { get; set; } = string.Empty;

    public List<AssetAuditItemDto> MatchedItems { get; set; } = new();

    public List<AssetAuditItemDto> DisplacedItems { get; set; } = new();

    public List<AssetAuditItemDto> MissingItems { get; set; } = new();

    public List<AssetAuditItemDto> UnexpectedItems { get; set; } = new();
}
