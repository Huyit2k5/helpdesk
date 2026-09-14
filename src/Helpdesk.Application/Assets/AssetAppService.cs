using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Helpdesk.Assets.Dtos;
using Helpdesk.Categories;
using Helpdesk.Permissions;
using Helpdesk.Tickets;
using Helpdesk.TicketStatuses;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;

namespace Helpdesk.Assets;

[Authorize(HelpdeskPermissions.Assets.Default)]
public class AssetAppService : HelpdeskAppService, IAssetAppService
{
    private readonly IRepository<Asset, Guid> _assetRepository;
    private readonly IRepository<AssetActivity, Guid> _activityRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketStatus, Guid> _statusRepository;
    private readonly IRepository<AssetMaintenance, Guid> _maintenanceRepository;
    private readonly AssetManager _assetManager;
    private readonly IClock _clock;

    public AssetAppService(
        IRepository<Asset, Guid> assetRepository,
        IRepository<AssetActivity, Guid> activityRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketStatus, Guid> statusRepository,
        IRepository<AssetMaintenance, Guid> maintenanceRepository,
        AssetManager assetManager,
        IClock clock)
    {
        _assetRepository = assetRepository;
        _activityRepository = activityRepository;
        _ticketRepository = ticketRepository;
        _statusRepository = statusRepository;
        _maintenanceRepository = maintenanceRepository;
        _assetManager = assetManager;
        _clock = clock;
    }

    public async Task<PagedResultDto<AssetDto>> GetListAsync(GetAssetsInput input)
    {
        var queryable = await _assetRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            queryable = queryable.Where(a =>
                a.AssetTag.ToLower().Contains(filter) ||
                a.Name.ToLower().Contains(filter) ||
                (a.SerialNumber != null && a.SerialNumber.ToLower().Contains(filter)) ||
                (a.Model != null && a.Model.ToLower().Contains(filter)) ||
                (a.Manufacturer != null && a.Manufacturer.ToLower().Contains(filter)) ||
                (a.Location != null && a.Location.ToLower().Contains(filter)) ||
                (a.AssignedToUserName != null && a.AssignedToUserName.ToLower().Contains(filter)) ||
                (a.Department != null && a.Department.ToLower().Contains(filter)));
        }

        if (input.AssetType.HasValue)
        {
            queryable = queryable.Where(a => a.AssetType == input.AssetType.Value);
        }

        if (input.Status.HasValue)
        {
            queryable = queryable.Where(a => a.Status == input.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Department))
        {
            queryable = queryable.Where(a => a.Department == input.Department);
        }

        if (input.AssignedToUserId.HasValue)
        {
            queryable = queryable.Where(a => a.AssignedToUserId == input.AssignedToUserId.Value);
        }

        if (input.WarrantyExpiringSoon == true)
        {
            var now = _clock.Now;
            var soon = now.AddDays(30);
            queryable = queryable.Where(a =>
                a.WarrantyExpiryDate.HasValue &&
                a.WarrantyExpiryDate.Value >= now &&
                a.WarrantyExpiryDate.Value <= soon);
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var sorting = string.IsNullOrWhiteSpace(input.Sorting) ? "CreationTime DESC" : input.Sorting;
        queryable = queryable
            .OrderBy(sorting)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var items = await AsyncExecuter.ToListAsync(queryable);
        var assetIds = items.Select(i => i.Id).ToList();

        // Get count of open tickets for these assets
        var ticketsQuery = await _ticketRepository.GetQueryableAsync();
        var statusesQuery = await _statusRepository.GetQueryableAsync();

        var openStatusList = await AsyncExecuter.ToListAsync(statusesQuery.Where(s => s.StatusGroup != StatusGroup.Closed));
        var openStatusIds = openStatusList.Select(s => s.Id).ToList();

        var openTickets = await AsyncExecuter.ToListAsync(
            ticketsQuery.Where(t => t.AssetId.HasValue && assetIds.Contains(t.AssetId.Value) && openStatusIds.Contains(t.StatusId))
        );

        var ticketCounts = openTickets
            .GroupBy(t => t.AssetId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var dtos = items.Select(a =>
        {
            var dto = MapToDto(a);
            if (ticketCounts.TryGetValue(a.Id, out var count))
            {
                dto.OpenTicketCount = count;
            }
            return dto;
        }).ToList();

        return new PagedResultDto<AssetDto>(totalCount, dtos);
    }

    public async Task<AssetDetailDto> GetAsync(Guid id)
    {
        var asset = await _assetRepository.GetAsync(id);
        var dto = new AssetDetailDto
        {
            Id = asset.Id,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            AssetType = asset.AssetType,
            AssetTypeName = GetAssetTypeName(asset.AssetType),
            Status = asset.Status,
            StatusName = GetStatusName(asset.Status),
            SerialNumber = asset.SerialNumber,
            Model = asset.Model,
            Manufacturer = asset.Manufacturer,
            Location = asset.Location,
            PurchaseDate = asset.PurchaseDate,
            WarrantyExpiryDate = asset.WarrantyExpiryDate,
            PurchaseCost = asset.PurchaseCost,
            AssignedToUserId = asset.AssignedToUserId,
            AssignedToUserName = asset.AssignedToUserName,
            AssignedToUserEmail = asset.AssignedToUserEmail,
            Department = asset.Department,
            AssignedDate = asset.AssignedDate,
            Specifications = asset.Specifications,
            Notes = asset.Notes,
            IsHandoverConfirmed = asset.IsHandoverConfirmed,
            HandoverConfirmedDate = asset.HandoverConfirmedDate,
            HandoverNotes = asset.HandoverNotes,
            CreationTime = asset.CreationTime,
            CreatorId = asset.CreatorId,
            LastModificationTime = asset.LastModificationTime,
            LastModifierId = asset.LastModifierId
        };

        // Load activities
        var actQuery = await _activityRepository.GetQueryableAsync();
        var activities = await AsyncExecuter.ToListAsync(
            actQuery.Where(x => x.AssetId == id).OrderByDescending(x => x.CreationTime)
        );

        dto.Activities = activities.Select(a => new AssetActivityDto
        {
            Id = a.Id,
            AssetId = a.AssetId,
            ActivityType = a.ActivityType,
            ActivityTypeName = GetActivityTypeName(a.ActivityType),
            Title = a.Title,
            Description = a.Description,
            PerformedByUserId = a.PerformedByUserId,
            PerformedByUserName = a.PerformedByUserName,
            RelatedTicketId = a.RelatedTicketId,
            CreationTime = a.CreationTime
        }).ToList();

        // Load tickets
        var ticketQuery = await _ticketRepository.GetQueryableAsync();
        var statusQuery = await _statusRepository.GetQueryableAsync();

        var tickets = await AsyncExecuter.ToListAsync(ticketQuery.Where(x => x.AssetId == id).OrderByDescending(t => t.CreationTime));
        var statusList = await AsyncExecuter.ToListAsync(statusQuery);
        var statusDict = statusList.ToDictionary(s => s.Id, s => s);

        dto.Tickets = tickets.Select(t =>
        {
            statusDict.TryGetValue(t.StatusId, out var st);
            return new AssetTicketDto
            {
                Id = t.Id,
                TicketNumber = t.TicketNumber,
                Title = t.Title,
                StatusName = st?.Name ?? string.Empty,
                RequesterName = t.RequesterName,
                CreationTime = t.CreationTime
            };
        }).ToList();

        dto.OpenTicketCount = tickets.Count(t => statusDict.TryGetValue(t.StatusId, out var st) && st.StatusGroup != StatusGroup.Closed);

        // Load maintenance records & TCO
        var maintQuery = await _maintenanceRepository.GetQueryableAsync();
        var maintenances = await AsyncExecuter.ToListAsync(
            maintQuery.Where(x => x.AssetId == id).OrderByDescending(x => x.CreationTime)
        );

        var ticketNums = tickets.ToDictionary(t => t.Id, t => t.TicketNumber);

        dto.Maintenances = maintenances.Select(m => new AssetMaintenanceDto
        {
            Id = m.Id,
            AssetId = m.AssetId,
            AssetTag = asset.AssetTag,
            AssetName = asset.Name,
            MaintenanceType = m.MaintenanceType,
            MaintenanceTypeName = GetMaintenanceTypeName(m.MaintenanceType),
            Status = m.Status,
            StatusName = GetMaintenanceStatusName(m.Status),
            Title = m.Title,
            Description = m.Description,
            ServiceProvider = m.ServiceProvider,
            TrackingNumber = m.TrackingNumber,
            StartDate = m.StartDate,
            ExpectedCompletionDate = m.ExpectedCompletionDate,
            ActualCompletionDate = m.ActualCompletionDate,
            EstimatedCost = m.EstimatedCost,
            ActualCost = m.ActualCost,
            ReplacedParts = m.ReplacedParts,
            PartsWarrantyExpiry = m.PartsWarrantyExpiry,
            Notes = m.Notes,
            RelatedTicketId = m.RelatedTicketId,
            RelatedTicketNumber = m.RelatedTicketId.HasValue && ticketNums.TryGetValue(m.RelatedTicketId.Value, out var num) ? num : null,
            PerformedByUserId = m.PerformedByUserId,
            PerformedByUserName = m.PerformedByUserName,
            CreationTime = m.CreationTime
        }).ToList();

        var purchaseCost = asset.PurchaseCost ?? 0;
        var totalMaintCost = asset.TotalMaintenanceCost;
        if (totalMaintCost == 0 && maintenances.Any())
        {
            totalMaintCost = maintenances.Where(m => m.ActualCost.HasValue).Sum(m => m.ActualCost!.Value);
        }

        var repairPct = purchaseCost > 0 ? (double)(totalMaintCost / purchaseCost * 100) : 0;
        var recommendation = repairPct >= 50
            ? "Khuyến nghị thanh lý (Chi phí sửa chữa vượt quá 50% giá trị thiết bị)"
            : repairPct >= 30
                ? "Cần theo dõi chi phí (Chi phí sửa chữa chiếm trên 30% giá máy)"
                : "Kinh tế (Thiết bị hoạt động ổn định, chi phí bảo dưỡng tối ưu)";

        var recommendationColor = repairPct >= 50 ? "danger" : repairPct >= 30 ? "warning" : "success";

        dto.TcoSummary = new AssetTcoSummaryDto
        {
            AssetId = asset.Id,
            AssetTag = asset.AssetTag,
            AssetName = asset.Name,
            PurchaseCost = purchaseCost,
            TotalMaintenanceCost = totalMaintCost,
            TotalCostOfOwnership = purchaseCost + totalMaintCost,
            MaintenanceCount = maintenances.Count(m => m.Status == MaintenanceStatus.Completed),
            RepairCostPercentage = Math.Round(repairPct, 1),
            Recommendation = recommendation,
            RecommendationColor = recommendationColor,
            LastMaintenanceDate = asset.LastMaintenanceDate,
            NextMaintenanceDate = asset.NextMaintenanceDate,
            IsDueForMaintenance = asset.NextMaintenanceDate.HasValue && asset.NextMaintenanceDate.Value <= DateTime.Now.AddDays(15)
        };

        dto.TotalMaintenanceCost = totalMaintCost;
        dto.LastMaintenanceDate = asset.LastMaintenanceDate ?? maintenances.FirstOrDefault(m => m.Status == MaintenanceStatus.Completed)?.ActualCompletionDate;
        dto.NextMaintenanceDate = asset.NextMaintenanceDate;
        dto.MaintenanceIntervalMonths = asset.MaintenanceIntervalMonths;

        return dto;
    }

    [Authorize(HelpdeskPermissions.Assets.Create)]
    public async Task<AssetDto> CreateAsync(CreateAssetDto input)
    {
        var asset = await _assetManager.CreateAsync(
            input.AssetTag,
            input.Name,
            input.AssetType,
            input.Status,
            input.SerialNumber,
            input.Model,
            input.Manufacturer,
            input.Location,
            input.PurchaseDate,
            input.WarrantyExpiryDate,
            input.PurchaseCost,
            input.Specifications,
            input.Notes,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        await _assetRepository.InsertAsync(asset);
        return MapToDto(asset);
    }

    [Authorize(HelpdeskPermissions.Assets.Edit)]
    public async Task<AssetDto> UpdateAsync(Guid id, UpdateAssetDto input)
    {
        var asset = await _assetRepository.GetAsync(id);

        await _assetManager.CheckSerialNumberUniqueAsync(input.SerialNumber, id);

        asset.UpdateInfo(
            input.Name,
            input.AssetType,
            input.SerialNumber,
            input.Model,
            input.Manufacturer,
            input.Location,
            input.PurchaseDate,
            input.WarrantyExpiryDate,
            input.PurchaseCost,
            input.Specifications,
            input.Notes
        );

        await _assetRepository.UpdateAsync(asset);
        return MapToDto(asset);
    }

    [Authorize(HelpdeskPermissions.Assets.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _assetRepository.DeleteAsync(id);
    }

    [Authorize(HelpdeskPermissions.Assets.Assign)]
    public async Task AssignAsync(Guid id, AssignAssetDto input)
    {
        var asset = await _assetRepository.GetAsync(id);
        await _assetManager.AssignAsync(
            asset,
            input.UserId,
            input.UserName,
            input.UserEmail,
            input.Department,
            input.Notes,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        await _assetRepository.UpdateAsync(asset);
    }

    [Authorize(HelpdeskPermissions.Assets.Assign)]
    public async Task ReturnAsync(Guid id, ReturnAssetDto? input)
    {
        var asset = await _assetRepository.GetAsync(id);
        await _assetManager.ReturnToStockAsync(
            asset,
            input?.Notes,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        await _assetRepository.UpdateAsync(asset);
    }

    [Authorize(HelpdeskPermissions.Assets.ChangeStatus)]
    public async Task ChangeStatusAsync(Guid id, ChangeAssetStatusDto input)
    {
        var asset = await _assetRepository.GetAsync(id);
        await _assetManager.ChangeStatusAsync(
            asset,
            input.Status,
            input.Reason,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        await _assetRepository.UpdateAsync(asset);
    }

    public async Task<AssetKpiDto> GetKpisAsync()
    {
        var queryable = await _assetRepository.GetQueryableAsync();
        var now = _clock.Now;
        var soon = now.AddDays(30);

        var allAssets = await AsyncExecuter.ToListAsync(queryable);

        var total = allAssets.Count;
        var inStock = allAssets.Count(a => a.Status == AssetStatus.InStock);
        var assigned = allAssets.Count(a => a.Status == AssetStatus.Assigned);
        var underRepair = allAssets.Count(a => a.Status == AssetStatus.UnderRepair);
        var expiringSoon = allAssets.Count(a =>
            a.WarrantyExpiryDate.HasValue &&
            a.WarrantyExpiryDate.Value >= now &&
            a.WarrantyExpiryDate.Value <= soon);

        var stockByType = Enum.GetValues<AssetType>().Select(t => new AssetTypeStockDto
        {
            AssetType = t,
            AssetTypeName = GetAssetTypeName(t),
            TotalCount = allAssets.Count(a => a.AssetType == t),
            InStockCount = allAssets.Count(a => a.AssetType == t && a.Status == AssetStatus.InStock),
            AssignedCount = allAssets.Count(a => a.AssetType == t && a.Status == AssetStatus.Assigned)
        }).Where(s => s.TotalCount > 0).ToList();

        return new AssetKpiDto
        {
            TotalAssets = total,
            InStockCount = inStock,
            AssignedCount = assigned,
            UnderRepairCount = underRepair,
            WarrantyExpiringSoonCount = expiringSoon,
            StockByType = stockByType
        };
    }

    [AllowAnonymous]
    public async Task<List<AssetDto>> GetLookupAsync()
    {
        var queryable = await _assetRepository.GetQueryableAsync();
        var items = await AsyncExecuter.ToListAsync(
            queryable.OrderBy(a => a.Name).Take(200)
        );

        return items.Select(MapToDto).ToList();
    }

    [AllowAnonymous]
    public async Task<List<AssetDto>> GetMyAssetsAsync()
    {
        var queryable = await _assetRepository.GetQueryableAsync();

        if (CurrentUser.IsAuthenticated)
        {
            var userId = CurrentUser.Id;
            var email = CurrentUser.Email;

            queryable = queryable.Where(a =>
                (userId.HasValue && a.AssignedToUserId == userId.Value) ||
                (!string.IsNullOrEmpty(email) && a.AssignedToUserEmail == email));
        }
        else
        {
            queryable = queryable.Where(a => a.Status == AssetStatus.Assigned).Take(10);
        }

        var items = await AsyncExecuter.ToListAsync(queryable.OrderBy(a => a.Name));
        return items.Select(MapToDto).ToList();
    }

    private static AssetDto MapToDto(Asset a)
    {
        return new AssetDto
        {
            Id = a.Id,
            AssetTag = a.AssetTag,
            Name = a.Name,
            AssetType = a.AssetType,
            AssetTypeName = GetAssetTypeName(a.AssetType),
            Status = a.Status,
            StatusName = GetStatusName(a.Status),
            SerialNumber = a.SerialNumber,
            Model = a.Model,
            Manufacturer = a.Manufacturer,
            Location = a.Location,
            PurchaseDate = a.PurchaseDate,
            WarrantyExpiryDate = a.WarrantyExpiryDate,
            PurchaseCost = a.PurchaseCost,
            AssignedToUserId = a.AssignedToUserId,
            AssignedToUserName = a.AssignedToUserName,
            AssignedToUserEmail = a.AssignedToUserEmail,
            Department = a.Department,
            AssignedDate = a.AssignedDate,
            Specifications = a.Specifications,
            Notes = a.Notes,
            IsHandoverConfirmed = a.IsHandoverConfirmed,
            HandoverConfirmedDate = a.HandoverConfirmedDate,
            HandoverNotes = a.HandoverNotes,
            TotalMaintenanceCost = a.TotalMaintenanceCost,
            LastMaintenanceDate = a.LastMaintenanceDate,
            NextMaintenanceDate = a.NextMaintenanceDate,
            MaintenanceIntervalMonths = a.MaintenanceIntervalMonths,
            CreationTime = a.CreationTime,
            CreatorId = a.CreatorId,
            LastModificationTime = a.LastModificationTime,
            LastModifierId = a.LastModifierId
        };
    }

    private static string GetAssetTypeName(AssetType type)
    {
        return type switch
        {
            AssetType.Laptop => "Máy tính xách tay (Laptop)",
            AssetType.Desktop => "Máy tính để bàn (PC)",
            AssetType.Monitor => "Màn hình hiển thị",
            AssetType.NetworkDevice => "Thiết bị mạng (Router/Switch/AP)",
            AssetType.PrinterPeripheral => "Máy in & Thiết bị ngoại vi",
            AssetType.ServerStorage => "Máy chủ & Lưu trữ (Server)",
            AssetType.SoftwareLicense => "Bản quyền phần mềm",
            AssetType.MobileDevice => "Thiết bị di động (Tablet/Phone)",
            _ => "Thiết bị khác"
        };
    }

    private static string GetStatusName(AssetStatus status)
    {
        return status switch
        {
            AssetStatus.InStock => "Trong kho lưu trữ",
            AssetStatus.Assigned => "Đang cấp phát sử dụng",
            AssetStatus.UnderRepair => "Đang sửa chữa / Bảo hành",
            AssetStatus.Reserved => "Đã giữ chỗ / Đặt trước",
            AssetStatus.Retired => "Đã thanh lý / Ngừng sử dụng",
            AssetStatus.LostStolen => "Thất lạc / Báo mất",
            _ => "Không xác định"
        };
    }

    private static string GetActivityTypeName(AssetActivityType type)
    {
        return type switch
        {
            AssetActivityType.Created => "Khởi tạo thiết bị",
            AssetActivityType.Assigned => "Cấp phát cho nhân sự",
            AssetActivityType.Returned => "Thu hồi về kho",
            AssetActivityType.StatusChanged => "Thay đổi trạng thái",
            AssetActivityType.SentToRepair => "Gửi đi bảo dưỡng / Sửa chữa",
            AssetActivityType.Repaired => "Hoàn tất bảo dưỡng / Nhập kho",
            AssetActivityType.TicketLinked => "Liên kết yêu cầu hỗ trợ",
            AssetActivityType.NoteAdded => "Thêm ghi chú",
            AssetActivityType.HandoverConfirmed => "Ký nhận bàn giao thiết bị",
            AssetActivityType.MaintenanceStarted => "Bắt đầu bảo trì / sửa chữa",
            AssetActivityType.MaintenanceCompleted => "Hoàn tất bảo trì / sửa chữa",
            _ => "Hoạt động"
        };
    }

    private static string GetMaintenanceTypeName(MaintenanceType type)
    {
        return type switch
        {
            MaintenanceType.Repair => "Sửa chữa sự cố",
            MaintenanceType.Preventive => "Bảo trì định kỳ",
            MaintenanceType.Upgrade => "Nâng cấp linh kiện",
            MaintenanceType.Inspection => "Kiểm tra kỹ thuật",
            _ => "Bảo trì"
        };
    }

    private static string GetMaintenanceStatusName(MaintenanceStatus status)
    {
        return status switch
        {
            MaintenanceStatus.Draft => "Dự thảo / Chờ gửi",
            MaintenanceStatus.InProgress => "Đang sửa chữa",
            MaintenanceStatus.Completed => "Đã hoàn tất",
            MaintenanceStatus.Cancelled => "Đã hủy",
            _ => "Không xác định"
        };
    }

    public async Task<AssetReceiptDto> GetReceiptAsync(Guid id, string? type = "handover")
    {
        var asset = await _assetRepository.GetAsync(id);
        var isReturn = string.Equals(type, "return", StringComparison.OrdinalIgnoreCase);

        var receiptNumber = isReturn
            ? $"BB-TH/{asset.CreationTime:yyyy}/{asset.AssetTag}"
            : $"BB-BG/{asset.CreationTime:yyyy}/{asset.AssetTag}";

        var receiptTitle = isReturn
            ? "BIÊN BẢN THU HỒI THIẾT BỊ CÔNG NGHỆ THÔNG TIN"
            : "BIÊN BẢN BÀN GIAO THIẾT BỊ CÔNG NGHỆ THÔNG TIN";

        var currentUser = CurrentUser.UserName ?? CurrentUser.Name ?? "Quản trị viên IT";
        var currentEmail = CurrentUser.Email ?? "it-support@company.com";

        return new AssetReceiptDto
        {
            AssetId = asset.Id,
            ReceiptNumber = receiptNumber,
            ReceiptType = isReturn ? "return" : "handover",
            Title = receiptTitle,
            GeneratedDate = DateTime.Now,

            GiverName = isReturn ? (asset.AssignedToUserName ?? "Nhân viên sử dụng") : currentUser,
            GiverRole = isReturn ? (asset.Department ?? "Nhân viên bàn giao") : "Đại diện Bộ phận Kỹ thuật & CNTT",
            GiverEmail = isReturn ? (asset.AssignedToUserEmail ?? string.Empty) : currentEmail,

            ReceiverName = isReturn ? currentUser : (asset.AssignedToUserName ?? "Chưa chỉ định"),
            ReceiverEmail = isReturn ? currentEmail : (asset.AssignedToUserEmail ?? string.Empty),
            ReceiverDepartment = isReturn ? "Phòng Công nghệ Thông tin" : (asset.Department ?? "Chưa phân bổ"),

            AssetTag = asset.AssetTag,
            AssetName = asset.Name,
            AssetTypeName = GetAssetTypeName(asset.AssetType),
            Model = asset.Model,
            SerialNumber = asset.SerialNumber,
            Manufacturer = asset.Manufacturer,
            Specifications = asset.Specifications,
            Location = asset.Location,
            AssignedDate = asset.AssignedDate,
            WarrantyExpiryDate = asset.WarrantyExpiryDate,
            Condition = isReturn ? "Đã thu hồi, kiểm tra tình trạng ngoại quan và hoạt động" : "Thiết bị hoạt động tốt, đầy đủ linh phụ kiện",
            Accessories = "01 Máy tính / Thiết bị, 01 Bộ sạc nguồn chính hãng, 01 Chuột máy tính, 01 Túi bảo vệ chống sốc",
            Notes = asset.Notes,
            IsConfirmed = asset.IsHandoverConfirmed,
            ConfirmedDate = asset.HandoverConfirmedDate
        };
    }

    public async Task<AssetDto?> GetByAssetTagAsync(string assetTag)
    {
        if (string.IsNullOrWhiteSpace(assetTag)) return null;

        var cleanTag = assetTag.Trim();
        var queryable = await _assetRepository.GetQueryableAsync();
        var asset = await AsyncExecuter.FirstOrDefaultAsync(
            queryable.Where(a => a.AssetTag == cleanTag || a.SerialNumber == cleanTag)
        );

        return asset == null ? null : MapToDto(asset);
    }
}
