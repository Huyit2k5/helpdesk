using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Assets;

public class AssetAuditSession : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string AuditCode { get; set; } = string.Empty;

    public string? ScopeDepartment { get; set; }

    public string? ScopeLocation { get; set; }

    public AssetAuditStatus Status { get; set; } = AssetAuditStatus.Draft;

    public DateTime StartDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public int TotalExpectedCount { get; set; }

    public int ScannedCount { get; set; }

    public int MatchedCount { get; set; }

    public int DisplacedCount { get; set; }

    public int MissingCount { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<AssetAuditItem> Items { get; set; } = new List<AssetAuditItem>();

    protected AssetAuditSession()
    {
    }

    public AssetAuditSession(
        Guid id,
        string title,
        string auditCode,
        string? scopeDepartment = null,
        string? scopeLocation = null,
        string? notes = null,
        DateTime? startDate = null)
        : base(id)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), 256);
        AuditCode = Check.NotNullOrWhiteSpace(auditCode, nameof(auditCode), 64);
        ScopeDepartment = scopeDepartment;
        ScopeLocation = scopeLocation;
        Notes = notes;
        StartDate = startDate ?? DateTime.Now;
        Status = AssetAuditStatus.InProgress;
    }

    public void Start()
    {
        if (Status != AssetAuditStatus.Draft)
        {
            return;
        }
        Status = AssetAuditStatus.InProgress;
        StartDate = DateTime.Now;
    }

    public void Complete()
    {
        Status = AssetAuditStatus.Completed;
        CompletedDate = DateTime.Now;
    }

    public void Cancel()
    {
        Status = AssetAuditStatus.Cancelled;
        CompletedDate = DateTime.Now;
    }

    public void UpdateCounts(int total, int scanned, int matched, int displaced, int missing)
    {
        TotalExpectedCount = total;
        ScannedCount = scanned;
        MatchedCount = matched;
        DisplacedCount = displaced;
        MissingCount = missing;
    }
}
