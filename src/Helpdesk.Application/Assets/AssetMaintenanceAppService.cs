using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Helpdesk.Assets.Dtos;
using Helpdesk.Permissions;
using Helpdesk.Tickets;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Assets;

[Authorize(HelpdeskPermissions.Assets.Default)]
public class AssetMaintenanceAppService : HelpdeskAppService, IAssetMaintenanceAppService
{
    private readonly IRepository<AssetMaintenance, Guid> _maintenanceRepository;
    private readonly IRepository<Asset, Guid> _assetRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly AssetManager _assetManager;

    public AssetMaintenanceAppService(
        IRepository<AssetMaintenance, Guid> maintenanceRepository,
        IRepository<Asset, Guid> assetRepository,
        IRepository<Ticket, Guid> ticketRepository,
        AssetManager assetManager)
    {
        _maintenanceRepository = maintenanceRepository;
        _assetRepository = assetRepository;
        _ticketRepository = ticketRepository;
        _assetManager = assetManager;
    }

    public async Task<PagedResultDto<AssetMaintenanceDto>> GetListAsync(GetAssetMaintenanceListInput input)
    {
        var query = await _maintenanceRepository.GetQueryableAsync();

        if (input.AssetId.HasValue)
        {
            query = query.Where(m => m.AssetId == input.AssetId.Value);
        }

        if (input.MaintenanceType.HasValue)
        {
            query = query.Where(m => m.MaintenanceType == input.MaintenanceType.Value);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(m => m.Status == input.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(m =>
                m.Title.ToLower().Contains(filter) ||
                (m.ServiceProvider != null && m.ServiceProvider.ToLower().Contains(filter)) ||
                (m.TrackingNumber != null && m.TrackingNumber.ToLower().Contains(filter)) ||
                (m.ReplacedParts != null && m.ReplacedParts.ToLower().Contains(filter)));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = !string.IsNullOrWhiteSpace(input.Sorting)
            ? query.OrderBy(input.Sorting)
            : query.OrderByDescending(m => m.CreationTime);

        query = query.PageBy(input);

        var list = await AsyncExecuter.ToListAsync(query);
        var dtos = await MapToDtosAsync(list);

        return new PagedResultDto<AssetMaintenanceDto>(totalCount, dtos);
    }

    public async Task<AssetMaintenanceDto> GetAsync(Guid id)
    {
        var maintenance = await _maintenanceRepository.GetAsync(id);
        var dtos = await MapToDtosAsync(new List<AssetMaintenance> { maintenance });
        return dtos.First();
    }

    public async Task<List<AssetMaintenanceDto>> GetByAssetIdAsync(Guid assetId)
    {
        var query = await _maintenanceRepository.GetQueryableAsync();
        var list = await AsyncExecuter.ToListAsync(
            query.Where(m => m.AssetId == assetId).OrderByDescending(m => m.CreationTime)
        );
        return await MapToDtosAsync(list);
    }

    public async Task<AssetMaintenanceDto> CreateAsync(CreateAssetMaintenanceDto input)
    {
        var asset = await _assetRepository.GetAsync(input.AssetId);

        var maintenance = new AssetMaintenance(
            GuidGenerator.Create(),
            asset.Id,
            input.MaintenanceType,
            input.Title,
            input.Description,
            input.ServiceProvider,
            input.TrackingNumber,
            input.StartDate ?? DateTime.Now,
            input.ExpectedCompletionDate,
            input.EstimatedCost,
            input.RelatedTicketId,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        await _maintenanceRepository.InsertAsync(maintenance);

        if (input.SetAssetUnderRepair)
        {
            asset.SendToMaintenance(input.Title);
            await _assetRepository.UpdateAsync(asset);
        }

        await _assetManager.LogMaintenanceStartedAsync(
            asset,
            maintenance,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        return (await MapToDtosAsync(new List<AssetMaintenance> { maintenance })).First();
    }

    public async Task<AssetMaintenanceDto> CompleteAsync(Guid id, CompleteAssetMaintenanceDto input)
    {
        var maintenance = await _maintenanceRepository.GetAsync(id);
        if (maintenance.Status == MaintenanceStatus.Completed)
        {
            throw new UserFriendlyException("Phiếu bảo dưỡng/sửa chữa này đã được hoàn tất trước đó.");
        }

        var asset = await _assetRepository.GetAsync(maintenance.AssetId);

        maintenance.Complete(
            input.ActualCost,
            input.ActualCompletionDate ?? DateTime.Now,
            input.ReplacedParts,
            input.PartsWarrantyExpiry,
            input.Notes
        );

        asset.CompleteMaintenance(
            input.ActualCost,
            input.ReturnToStock,
            input.NextMaintenanceDate
        );

        await _maintenanceRepository.UpdateAsync(maintenance);
        await _assetRepository.UpdateAsync(asset);

        await _assetManager.LogMaintenanceCompletedAsync(
            asset,
            maintenance,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        return (await MapToDtosAsync(new List<AssetMaintenance> { maintenance })).First();
    }

    public async Task<AssetMaintenanceDto> CancelAsync(Guid id, string? reason)
    {
        var maintenance = await _maintenanceRepository.GetAsync(id);
        maintenance.Cancel(reason);
        await _maintenanceRepository.UpdateAsync(maintenance);

        return (await MapToDtosAsync(new List<AssetMaintenance> { maintenance })).First();
    }

    public async Task<AssetTcoSummaryDto> GetTcoSummaryAsync(Guid assetId)
    {
        var asset = await _assetRepository.GetAsync(assetId);
        var query = await _maintenanceRepository.GetQueryableAsync();
        var maintenances = await AsyncExecuter.ToListAsync(query.Where(m => m.AssetId == assetId));

        var purchaseCost = asset.PurchaseCost ?? 0;
        var totalMaintenanceCost = asset.TotalMaintenanceCost;
        if (totalMaintenanceCost == 0 && maintenances.Any())
        {
            totalMaintenanceCost = maintenances.Where(m => m.ActualCost.HasValue).Sum(m => m.ActualCost!.Value);
        }

        var totalTco = purchaseCost + totalMaintenanceCost;
        var maintenanceCount = maintenances.Count(m => m.Status == MaintenanceStatus.Completed);
        var repairPercentage = purchaseCost > 0 ? (double)(totalMaintenanceCost / purchaseCost * 100) : 0;

        string recommendation;
        string recommendationColor;

        if (repairPercentage >= 50)
        {
            recommendation = "Khuyến nghị thanh lý (Chi phí sửa chữa vượt quá 50% giá trị thiết bị)";
            recommendationColor = "danger";
        }
        else if (repairPercentage >= 30)
        {
            recommendation = "Cần theo dõi chi phí (Chi phí sửa chữa chiếm trên 30% giá máy)";
            recommendationColor = "warning";
        }
        else
        {
            recommendation = "Kinh tế (Thiết bị hoạt động ổn định, chi phí bảo dưỡng tối ưu)";
            recommendationColor = "success";
        }

        var isDue = asset.NextMaintenanceDate.HasValue && asset.NextMaintenanceDate.Value <= DateTime.Now.AddDays(15);

        return new AssetTcoSummaryDto
        {
            AssetId = asset.Id,
            AssetTag = asset.AssetTag,
            AssetName = asset.Name,
            PurchaseCost = purchaseCost,
            TotalMaintenanceCost = totalMaintenanceCost,
            TotalCostOfOwnership = totalTco,
            MaintenanceCount = maintenanceCount,
            RepairCostPercentage = Math.Round(repairPercentage, 1),
            Recommendation = recommendation,
            RecommendationColor = recommendationColor,
            LastMaintenanceDate = asset.LastMaintenanceDate,
            NextMaintenanceDate = asset.NextMaintenanceDate,
            IsDueForMaintenance = isDue
        };
    }

    public async Task<List<MaintenanceScheduleAlertDto>> GetScheduleAlertsAsync(int daysThreshold = 30)
    {
        var thresholdDate = DateTime.Now.AddDays(daysThreshold);
        var query = await _assetRepository.GetQueryableAsync();

        var dueAssets = await AsyncExecuter.ToListAsync(
            query.Where(a =>
                a.NextMaintenanceDate.HasValue &&
                a.NextMaintenanceDate.Value <= thresholdDate &&
                a.Status != AssetStatus.Retired &&
                a.Status != AssetStatus.LostStolen)
            .OrderBy(a => a.NextMaintenanceDate)
        );

        var now = DateTime.Now.Date;

        return dueAssets.Select(a =>
        {
            var dueDate = a.NextMaintenanceDate!.Value.Date;
            var daysDiff = (int)(dueDate - now).TotalDays;
            var isOverdue = daysDiff < 0;

            return new MaintenanceScheduleAlertDto
            {
                AssetId = a.Id,
                AssetTag = a.AssetTag,
                AssetName = a.Name,
                AssetTypeName = GetAssetTypeName(a.AssetType),
                Location = a.Location,
                AssignedToUserName = a.AssignedToUserName,
                NextMaintenanceDate = a.NextMaintenanceDate,
                DaysOverdueOrRemaining = Math.Abs(daysDiff),
                IsOverdue = isOverdue
            };
        }).ToList();
    }

    private async Task<List<AssetMaintenanceDto>> MapToDtosAsync(List<AssetMaintenance> entities)
    {
        if (!entities.Any()) return new List<AssetMaintenanceDto>();

        var assetIds = entities.Select(e => e.AssetId).Distinct().ToList();
        var assetQuery = await _assetRepository.GetQueryableAsync();
        var assets = await AsyncExecuter.ToListAsync(assetQuery.Where(a => assetIds.Contains(a.Id)));
        var assetDict = assets.ToDictionary(a => a.Id);

        var ticketIds = entities.Where(e => e.RelatedTicketId.HasValue).Select(e => e.RelatedTicketId!.Value).Distinct().ToList();
        var ticketDict = new Dictionary<Guid, string>();
        if (ticketIds.Any())
        {
            var ticketQuery = await _ticketRepository.GetQueryableAsync();
            var tickets = await AsyncExecuter.ToListAsync(ticketQuery.Where(t => ticketIds.Contains(t.Id)));
            ticketDict = tickets.ToDictionary(t => t.Id, t => t.TicketNumber);
        }

        return entities.Select(e =>
        {
            assetDict.TryGetValue(e.AssetId, out var asset);
            string? ticketNumber = null;
            if (e.RelatedTicketId.HasValue)
            {
                ticketDict.TryGetValue(e.RelatedTicketId.Value, out ticketNumber);
            }

            return new AssetMaintenanceDto
            {
                Id = e.Id,
                AssetId = e.AssetId,
                AssetTag = asset?.AssetTag ?? string.Empty,
                AssetName = asset?.Name ?? string.Empty,
                MaintenanceType = e.MaintenanceType,
                MaintenanceTypeName = GetMaintenanceTypeName(e.MaintenanceType),
                Status = e.Status,
                StatusName = GetMaintenanceStatusName(e.Status),
                Title = e.Title,
                Description = e.Description,
                ServiceProvider = e.ServiceProvider,
                TrackingNumber = e.TrackingNumber,
                StartDate = e.StartDate,
                ExpectedCompletionDate = e.ExpectedCompletionDate,
                ActualCompletionDate = e.ActualCompletionDate,
                EstimatedCost = e.EstimatedCost,
                ActualCost = e.ActualCost,
                ReplacedParts = e.ReplacedParts,
                PartsWarrantyExpiry = e.PartsWarrantyExpiry,
                Notes = e.Notes,
                RelatedTicketId = e.RelatedTicketId,
                RelatedTicketNumber = ticketNumber,
                PerformedByUserId = e.PerformedByUserId,
                PerformedByUserName = e.PerformedByUserName,
                CreationTime = e.CreationTime
            };
        }).ToList();
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

    private static string GetAssetTypeName(AssetType type)
    {
        return type switch
        {
            AssetType.Laptop => "Laptop",
            AssetType.Desktop => "Desktop",
            AssetType.Monitor => "Màn hình",
            AssetType.NetworkDevice => "Thiết bị mạng",
            AssetType.PrinterPeripheral => "Máy in / Ngoại vi",
            AssetType.ServerStorage => "Máy chủ / Lưu trữ",
            AssetType.SoftwareLicense => "Bản quyền phần mềm",
            AssetType.MobileDevice => "Thiết bị di động",
            _ => "Thiết bị khác"
        };
    }
}
