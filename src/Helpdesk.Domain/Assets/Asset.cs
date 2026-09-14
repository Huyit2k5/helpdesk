using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.Assets;

/// <summary>
/// Quản lý tài sản CNTT & Thiết bị (IT Asset Management - ITAM & CMDB)
/// </summary>
public class Asset : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string AssetTag { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public AssetType AssetType { get; private set; }

    public AssetStatus Status { get; private set; }

    public string? SerialNumber { get; private set; }

    public string? Model { get; private set; }

    public string? Manufacturer { get; private set; }

    public string? Location { get; private set; }

    public DateTime? PurchaseDate { get; private set; }

    public DateTime? WarrantyExpiryDate { get; private set; }

    public decimal? PurchaseCost { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public string? AssignedToUserName { get; private set; }

    public string? AssignedToUserEmail { get; private set; }

    public string? Department { get; private set; }

    public DateTime? AssignedDate { get; private set; }

    public string? Specifications { get; private set; }

    public string? Notes { get; private set; }

    public bool IsHandoverConfirmed { get; private set; }

    public DateTime? HandoverConfirmedDate { get; private set; }

    public string? HandoverNotes { get; private set; }

    public virtual ICollection<AssetActivity> Activities { get; private set; } = new List<AssetActivity>();

    protected Asset()
    {
        // For EF Core
    }

    public Asset(
        Guid id,
        string assetTag,
        string name,
        AssetType assetType,
        AssetStatus status = AssetStatus.InStock,
        string? serialNumber = null,
        string? model = null,
        string? manufacturer = null,
        string? location = null,
        DateTime? purchaseDate = null,
        DateTime? warrantyExpiryDate = null,
        decimal? purchaseCost = null,
        string? specifications = null,
        string? notes = null)
        : base(id)
    {
        AssetTag = Check.NotNullOrWhiteSpace(assetTag, nameof(assetTag), AssetConsts.MaxAssetTagLength);
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), AssetConsts.MaxNameLength);
        AssetType = assetType;
        Status = status;
        SerialNumber = serialNumber;
        Model = model;
        Manufacturer = manufacturer;
        Location = location;
        PurchaseDate = purchaseDate;
        WarrantyExpiryDate = warrantyExpiryDate;
        PurchaseCost = purchaseCost;
        Specifications = specifications;
        Notes = notes;
    }

    public void UpdateInfo(
        string name,
        AssetType assetType,
        string? serialNumber,
        string? model,
        string? manufacturer,
        string? location,
        DateTime? purchaseDate,
        DateTime? warrantyExpiryDate,
        decimal? purchaseCost,
        string? specifications,
        string? notes)
    {
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), AssetConsts.MaxNameLength);
        AssetType = assetType;
        SerialNumber = serialNumber;
        Model = model;
        Manufacturer = manufacturer;
        Location = location;
        PurchaseDate = purchaseDate;
        WarrantyExpiryDate = warrantyExpiryDate;
        PurchaseCost = purchaseCost;
        Specifications = specifications;
        Notes = notes;
    }

    public void AssignTo(Guid? userId, string? userName, string? userEmail, string? department, DateTime? assignDate = null)
    {
        AssignedToUserId = userId;
        AssignedToUserName = userName;
        AssignedToUserEmail = userEmail;
        Department = department;
        AssignedDate = assignDate ?? DateTime.UtcNow;
        Status = AssetStatus.Assigned;
        IsHandoverConfirmed = false;
        HandoverConfirmedDate = null;
        HandoverNotes = null;
    }

    public void ConfirmHandover(string? notes = null)
    {
        IsHandoverConfirmed = true;
        HandoverConfirmedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            HandoverNotes = notes;
        }
    }

    public void ReturnToStock()
    {
        AssignedToUserId = null;
        AssignedToUserName = null;
        AssignedToUserEmail = null;
        Department = null;
        AssignedDate = null;
        Status = AssetStatus.InStock;
        IsHandoverConfirmed = false;
        HandoverConfirmedDate = null;
        HandoverNotes = null;
    }

    public void ChangeStatus(AssetStatus newStatus)
    {
        Status = newStatus;
        if (newStatus == AssetStatus.InStock || newStatus == AssetStatus.Retired || newStatus == AssetStatus.LostStolen)
        {
            AssignedToUserId = null;
            AssignedToUserName = null;
            AssignedToUserEmail = null;
            Department = null;
            AssignedDate = null;
            IsHandoverConfirmed = false;
            HandoverConfirmedDate = null;
            HandoverNotes = null;
        }
    }
}
