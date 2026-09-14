using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Timing;

namespace Helpdesk.Assets;

public class AssetManager : DomainService
{
    private readonly IRepository<Asset, Guid> _assetRepository;
    private readonly IRepository<AssetActivity, Guid> _activityRepository;
    private readonly IClock _clock;
    private readonly IDataFilter _dataFilter;

    public AssetManager(
        IRepository<Asset, Guid> assetRepository,
        IRepository<AssetActivity, Guid> activityRepository,
        IClock clock,
        IDataFilter dataFilter)
    {
        _assetRepository = assetRepository;
        _activityRepository = activityRepository;
        _clock = clock;
        _dataFilter = dataFilter;
    }

    /// <summary>
    /// Sinh mã tài sản tự động AST-yyyyMMdd-XXXX
    /// </summary>
    public async Task<string> GenerateAssetTagAsync()
    {
        var now = _clock.Now;
        var prefix = $"AST-{now:yyyyMMdd}-";

        using (_dataFilter.Disable<ISoftDelete>())
        {
            var queryable = await _assetRepository.GetQueryableAsync();
            var countToday = queryable.Count(a => a.AssetTag.StartsWith(prefix));

            var nextSeq = countToday + 1;
            var candidate = $"{prefix}{nextSeq:D4}";

            while (queryable.Any(a => a.AssetTag == candidate))
            {
                nextSeq++;
                candidate = $"{prefix}{nextSeq:D4}";
            }

            return candidate;
        }
    }

    /// <summary>
    /// Kiểm tra tính duy nhất của số Serial nếu được cung cấp
    /// </summary>
    public async Task CheckSerialNumberUniqueAsync(string? serialNumber, Guid? currentAssetId = null)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return;
        }

        var queryable = await _assetRepository.GetQueryableAsync();
        var exists = queryable.Any(a => a.SerialNumber == serialNumber && (!currentAssetId.HasValue || a.Id != currentAssetId.Value));
        if (exists)
        {
            throw new UserFriendlyException($"Số Serial '{serialNumber}' đã tồn tại trong hệ thống tài sản!");
        }
    }

    /// <summary>
    /// Khởi tạo thiết bị tài sản mới và lưu activity Created
    /// </summary>
    public async Task<Asset> CreateAsync(
        string? assetTag,
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
        string? notes = null,
        Guid? creatorUserId = null,
        string? creatorUserName = null)
    {
        if (string.IsNullOrWhiteSpace(assetTag))
        {
            assetTag = await GenerateAssetTagAsync();
        }
        else
        {
            var queryable = await _assetRepository.GetQueryableAsync();
            if (queryable.Any(a => a.AssetTag == assetTag))
            {
                throw new UserFriendlyException($"Mã tài sản '{assetTag}' đã tồn tại trong hệ thống!");
            }
        }

        await CheckSerialNumberUniqueAsync(serialNumber);

        var asset = new Asset(
            GuidGenerator.Create(),
            assetTag,
            name,
            assetType,
            status,
            serialNumber,
            model,
            manufacturer,
            location,
            purchaseDate,
            warrantyExpiryDate,
            purchaseCost,
            specifications,
            notes
        );

        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.Created,
            title: "Khởi tạo thiết bị",
            description: $"Tài sản {asset.Name} ({asset.AssetTag}) được đưa vào hệ thống.",
            performedByUserId: creatorUserId,
            performedByUserName: creatorUserName
        );

        await _activityRepository.InsertAsync(activity);

        return asset;
    }

    /// <summary>
    /// Cấp phát thiết bị cho nhân sự
    /// </summary>
    public async Task AssignAsync(
        Asset asset,
        Guid? userId,
        string? userName,
        string? userEmail,
        string? department,
        string? notes,
        Guid? performerId,
        string? performerName)
    {
        asset.AssignTo(userId, userName, userEmail, department);

        var desc = $"Cấp phát cho: {userName ?? "N/A"} ({department ?? "N/A"}). Ghi chú: {notes ?? "Không có"}";
        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.Assigned,
            title: "Cấp phát thiết bị",
            description: desc,
            performedByUserId: performerId,
            performedByUserName: performerName
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Nhân sự ký nhận bàn giao thiết bị
    /// </summary>
    public async Task ConfirmHandoverAsync(
        Asset asset,
        string? notes,
        Guid? performerId,
        string? performerName)
    {
        asset.ConfirmHandover(notes);

        var desc = $"Nhân sự {performerName ?? asset.AssignedToUserName ?? "người dùng"} đã xác nhận ký nhận bàn giao thiết bị. Ghi chú: {notes ?? "Không có"}";
        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.HandoverConfirmed,
            title: "Ký nhận bàn giao trực tuyến",
            description: desc,
            performedByUserId: performerId,
            performedByUserName: performerName
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Thu hồi thiết bị về kho
    /// </summary>
    public async Task ReturnToStockAsync(
        Asset asset,
        string? notes,
        Guid? performerId,
        string? performerName)
    {
        var prevUser = asset.AssignedToUserName;
        asset.ReturnToStock();

        var desc = $"Thu hồi từ {prevUser ?? "người dùng"} về kho lưu trữ. Ghi chú: {notes ?? "Không có"}";
        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.Returned,
            title: "Thu hồi về kho",
            description: desc,
            performedByUserId: performerId,
            performedByUserName: performerName
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Đổi trạng thái thiết bị
    /// </summary>
    public async Task ChangeStatusAsync(
        Asset asset,
        AssetStatus newStatus,
        string? reason,
        Guid? performerId,
        string? performerName)
    {
        var oldStatus = asset.Status;
        if (oldStatus == newStatus)
        {
            return;
        }

        asset.ChangeStatus(newStatus);

        var actType = newStatus switch
        {
            AssetStatus.UnderRepair => AssetActivityType.SentToRepair,
            AssetStatus.InStock when oldStatus == AssetStatus.UnderRepair => AssetActivityType.Repaired,
            _ => AssetActivityType.StatusChanged
        };

        var title = newStatus switch
        {
            AssetStatus.UnderRepair => "Gửi đi bảo dưỡng / sửa chữa",
            AssetStatus.Retired => "Thanh lý / Ngừng sử dụng",
            AssetStatus.LostStolen => "Báo mất / Thất lạc",
            AssetStatus.InStock when oldStatus == AssetStatus.UnderRepair => "Hoàn thành sửa chữa - Nhập lại kho",
            _ => $"Đổi trạng thái: {oldStatus} ➔ {newStatus}"
        };

        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            actType,
            title: title,
            description: reason,
            performedByUserId: performerId,
            performedByUserName: performerName
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Ghi log hoạt động liên kết Ticket vào Asset
    /// </summary>
    public async Task LogTicketLinkedAsync(
        Guid assetId,
        Guid ticketId,
        string ticketNumber,
        string ticketTitle,
        Guid? performerId,
        string? performerName)
    {
        var activity = new AssetActivity(
            GuidGenerator.Create(),
            assetId,
            AssetActivityType.TicketLinked,
            title: $"Liên kết sự cố: {ticketNumber}",
            description: ticketTitle,
            performedByUserId: performerId,
            performedByUserName: performerName,
            relatedTicketId: ticketId
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Ghi log gửi bảo trì / sửa chữa thiết bị
    /// </summary>
    public async Task LogMaintenanceStartedAsync(
        Asset asset,
        AssetMaintenance maintenance,
        Guid? performerId,
        string? performerName)
    {
        var desc = $"Gửi: {maintenance.Title}. Đơn vị: {maintenance.ServiceProvider ?? "N/A"}. Mã phiếu: {maintenance.TrackingNumber ?? "N/A"}. Chi phí dự tính: {(maintenance.EstimatedCost.HasValue ? maintenance.EstimatedCost.Value.ToString("N0") + " VNĐ" : "Chưa xác định")}";
        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.MaintenanceStarted,
            title: "Bắt đầu bảo trì / sửa chữa",
            description: desc,
            performedByUserId: performerId,
            performedByUserName: performerName,
            relatedTicketId: maintenance.RelatedTicketId
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Ghi log hoàn tất bảo trì / nghiệm thu sửa chữa thiết bị
    /// </summary>
    public async Task LogMaintenanceCompletedAsync(
        Asset asset,
        AssetMaintenance maintenance,
        Guid? performerId,
        string? performerName)
    {
        var costStr = maintenance.ActualCost.HasValue ? maintenance.ActualCost.Value.ToString("N0") + " VNĐ" : "0 VNĐ";
        var partsStr = !string.IsNullOrEmpty(maintenance.ReplacedParts) ? $" | Linh kiện thay: {maintenance.ReplacedParts}" : string.Empty;
        var desc = $"Nghiệm thu hoàn tất: {maintenance.Title}. Chi phí thực tế: {costStr}{partsStr}. Trạng thái máy: {asset.Status}";

        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.MaintenanceCompleted,
            title: "Hoàn tất bảo trì / sửa chữa",
            description: desc,
            performedByUserId: performerId,
            performedByUserName: performerName,
            relatedTicketId: maintenance.RelatedTicketId
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Ghi log kiểm kê quét mã thiết bị trong đợt kiểm kê
    /// </summary>
    public async Task LogAuditScannedAsync(
        Asset asset,
        AssetAuditSession session,
        AuditItemResult result,
        string? scannedLocation,
        string? scannedUserName,
        Guid? performerId,
        string? performerName)
    {
        var resultText = result == AuditItemResult.Matched ? "Khớp hoàn toàn" : "Lệch vị trí / Người dùng";
        var desc = $"Đợt kiểm kê: {session.Title} ({session.AuditCode}). Kết quả: {resultText}. Vị trí quét: {scannedLocation ?? "N/A"}. Người sử dụng: {scannedUserName ?? "Chưa rõ"}";

        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.AuditScanned,
            title: $"Kiểm kê thiết bị ({session.AuditCode})",
            description: desc,
            performedByUserId: performerId,
            performedByUserName: performerName
        );

        await _activityRepository.InsertAsync(activity);
    }

    /// <summary>
    /// Ghi log cập nhật đối soát vị trí / người dùng sau kiểm kê
    /// </summary>
    public async Task LogAuditReconciledAsync(
        Asset asset,
        AssetAuditSession session,
        string? oldLocation,
        string? newLocation,
        string? oldUser,
        string? newUser,
        Guid? performerId,
        string? performerName)
    {
        var desc = $"Đối soát sau kiểm kê ({session.AuditCode}): Cập nhật vị trí từ '{oldLocation ?? "Trống"}' thành '{newLocation ?? "Trống"}', người dùng từ '{oldUser ?? "Trống"}' thành '{newUser ?? "Trống"}'";

        var activity = new AssetActivity(
            GuidGenerator.Create(),
            asset.Id,
            AssetActivityType.AuditReconciled,
            title: "Cập nhật dữ liệu sau kiểm kê",
            description: desc,
            performedByUserId: performerId,
            performedByUserName: performerName
        );

        await _activityRepository.InsertAsync(activity);
    }
}
