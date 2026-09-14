using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Assets;

public class AssetAuditItem : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid AuditSessionId { get; set; }

    public Guid AssetId { get; set; }

    public string? ExpectedLocation { get; set; }

    public Guid? ExpectedAssignedToUserId { get; set; }

    public string? ExpectedAssignedToUserName { get; set; }

    public string? ScannedLocation { get; set; }

    public Guid? ScannedAssignedToUserId { get; set; }

    public string? ScannedAssignedToUserName { get; set; }

    public AuditItemResult ResultStatus { get; set; } = AuditItemResult.Pending;

    public DateTime? ScannedTime { get; set; }

    public Guid? ScannedByUserId { get; set; }

    public string? ScannedByUserName { get; set; }

    public string? Notes { get; set; }

    public bool IsReconciled { get; set; } = false;

    public DateTime? ReconciledTime { get; set; }

    public virtual AssetAuditSession AuditSession { get; set; } = null!;

    public virtual Asset Asset { get; set; } = null!;

    protected AssetAuditItem()
    {
    }

    public AssetAuditItem(
        Guid id,
        Guid auditSessionId,
        Guid assetId,
        string? expectedLocation,
        Guid? expectedUserId,
        string? expectedUserName)
        : base(id)
    {
        AuditSessionId = auditSessionId;
        AssetId = assetId;
        ExpectedLocation = expectedLocation;
        ExpectedAssignedToUserId = expectedUserId;
        ExpectedAssignedToUserName = expectedUserName;
        ResultStatus = AuditItemResult.Pending;
    }

    public void RecordScan(
        string? scannedLocation,
        Guid? scannedUserId,
        string? scannedUserName,
        Guid? scannerUserId,
        string? scannerUserName,
        string? notes = null)
    {
        ScannedLocation = scannedLocation;
        ScannedAssignedToUserId = scannedUserId;
        ScannedAssignedToUserName = scannedUserName;
        ScannedByUserId = scannerUserId;
        ScannedByUserName = scannerUserName;
        ScannedTime = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes;
        }

        // Compare expected vs scanned
        bool locationMatches = string.Equals(ExpectedLocation?.Trim(), scannedLocation?.Trim(), StringComparison.OrdinalIgnoreCase);
        bool userMatches = ExpectedAssignedToUserId == scannedUserId;

        if (locationMatches && userMatches)
        {
            ResultStatus = AuditItemResult.Matched;
        }
        else
        {
            ResultStatus = AuditItemResult.Displaced;
        }
    }

    public void MarkReconciled()
    {
        IsReconciled = true;
        ReconciledTime = DateTime.Now;
    }
}
