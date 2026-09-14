using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Assets.Dtos;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Assets;

[Authorize(HelpdeskPermissions.Assets.Default)]
public class AssetAuditAppService : HelpdeskAppService, IAssetAuditAppService
{
    private readonly IRepository<AssetAuditSession, Guid> _sessionRepository;
    private readonly IRepository<AssetAuditItem, Guid> _itemRepository;
    private readonly IRepository<Asset, Guid> _assetRepository;
    private readonly AssetManager _assetManager;

    public AssetAuditAppService(
        IRepository<AssetAuditSession, Guid> sessionRepository,
        IRepository<AssetAuditItem, Guid> itemRepository,
        IRepository<Asset, Guid> assetRepository,
        AssetManager assetManager)
    {
        _sessionRepository = sessionRepository;
        _itemRepository = itemRepository;
        _assetRepository = assetRepository;
        _assetManager = assetManager;
    }

    public async Task<PagedResultDto<AssetAuditSessionDto>> GetListAsync(GetAssetAuditListInput input)
    {
        var query = await _sessionRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var f = input.Filter.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(f) || x.AuditCode.ToLower().Contains(f));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Department))
        {
            query = query.Where(x => x.ScopeDepartment == input.Department);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = query.OrderByDescending(x => x.CreationTime)
                     .Skip(input.SkipCount)
                     .Take(input.MaxResultCount);

        var sessions = await AsyncExecuter.ToListAsync(query);
        var dtos = sessions.Select(MapSessionToDto).ToList();

        return new PagedResultDto<AssetAuditSessionDto>(totalCount, dtos);
    }

    public async Task<AssetAuditSessionDto> GetAsync(Guid id)
    {
        var session = await _sessionRepository.GetAsync(id);
        var dto = MapSessionToDto(session);

        // Load items with asset info
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var assetQuery = await _assetRepository.GetQueryableAsync();

        var joined = from item in itemQuery.Where(x => x.AuditSessionId == id)
                     join asset in assetQuery on item.AssetId equals asset.Id into assetJoin
                     from a in assetJoin.DefaultIfEmpty()
                     orderby item.ScannedTime descending, item.CreationTime ascending
                     select new { Item = item, Asset = a };

        var list = await AsyncExecuter.ToListAsync(joined);

        dto.Items = list.Select(x => MapItemToDto(x.Item, x.Asset)).ToList();

        return dto;
    }

    public async Task<AssetAuditSessionDto> CreateAsync(CreateAssetAuditSessionDto input)
    {
        var count = await _sessionRepository.GetCountAsync();
        var auditCode = $"AUD-{DateTime.Now:yyyyMMdd}-{(count + 1):D3}";

        var session = new AssetAuditSession(
            GuidGenerator.Create(),
            input.Title,
            auditCode,
            input.ScopeDepartment,
            input.ScopeLocation,
            input.Notes
        );

        await _sessionRepository.InsertAsync(session);

        // Query active assets within scope
        var assetQuery = await _assetRepository.GetQueryableAsync();
        assetQuery = assetQuery.Where(a => a.Status != AssetStatus.Retired && a.Status != AssetStatus.LostStolen);

        if (!string.IsNullOrWhiteSpace(input.ScopeDepartment))
        {
            assetQuery = assetQuery.Where(a => a.Department == input.ScopeDepartment);
        }

        if (!string.IsNullOrWhiteSpace(input.ScopeLocation))
        {
            assetQuery = assetQuery.Where(a => a.Location == input.ScopeLocation);
        }

        var assets = await AsyncExecuter.ToListAsync(assetQuery);

        var items = new List<AssetAuditItem>();
        foreach (var a in assets)
        {
            items.Add(new AssetAuditItem(
                GuidGenerator.Create(),
                session.Id,
                a.Id,
                a.Location,
                a.AssignedToUserId,
                a.AssignedToUserName
            ));
        }

        if (items.Any())
        {
            await _itemRepository.InsertManyAsync(items);
        }

        session.UpdateCounts(
            total: items.Count,
            scanned: 0,
            matched: 0,
            displaced: 0,
            missing: items.Count
        );

        // Auto-start session immediately so users can scan without a separate step
        session.Start();
        await _sessionRepository.UpdateAsync(session);

        var resultDto = MapSessionToDto(session);
        resultDto.Items = items.Select(i => MapItemToDto(i, assets.FirstOrDefault(a => a.Id == i.AssetId))).ToList();

        return resultDto;
    }

    public async Task<AssetAuditSessionDto> StartAsync(Guid id)
    {
        var session = await _sessionRepository.GetAsync(id);
        session.Start();
        await _sessionRepository.UpdateAsync(session);
        return await GetAsync(id);
    }

    public async Task<ScanResultDto> ScanItemAsync(Guid id, ScanAuditItemInput input)
    {
        var session = await _sessionRepository.GetAsync(id);
        if (session.Status == AssetAuditStatus.Completed || session.Status == AssetAuditStatus.Cancelled)
        {
            throw new UserFriendlyException("Đợt kiểm kê này đã kết thúc hoặc bị hủy, không thể quét thêm thiết bị.");
        }

        var rawCode = (input.Code ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(rawCode))
        {
            return new ScanResultDto { Success = false, Message = "Mã quét không hợp lệ." };
        }

        // If code is a full URL like http://.../assets/{id}, extract the GUID or tag
        string normalizedCode = rawCode;
        if (rawCode.Contains("/assets/"))
        {
            var parts = rawCode.Split(new[] { "/assets/" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                normalizedCode = parts[1].Trim().Split('?')[0].Split('/')[0];
            }
        }

        var assetQuery = await _assetRepository.GetQueryableAsync();
        Asset? asset = null;

        if (Guid.TryParse(normalizedCode, out var assetGuid))
        {
            asset = await AsyncExecuter.FirstOrDefaultAsync(assetQuery.Where(a => a.Id == assetGuid));
        }

        if (asset == null)
        {
            asset = await AsyncExecuter.FirstOrDefaultAsync(
                assetQuery.Where(a => a.AssetTag.ToLower() == normalizedCode.ToLower() ||
                                      (a.SerialNumber != null && a.SerialNumber.ToLower() == normalizedCode.ToLower()))
            );
        }

        if (asset == null)
        {
            return new ScanResultDto
            {
                Success = false,
                Message = $"Không tìm thấy thiết bị nào có mã nhận diện: {rawCode}"
            };
        }

        // Find existing item in this audit session
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var item = await AsyncExecuter.FirstOrDefaultAsync(
            itemQuery.Where(x => x.AuditSessionId == id && x.AssetId == asset.Id)
        );

        bool isUnexpected = false;
        if (item == null)
        {
            // Asset is not in original audit scope -> Create unexpected item
            item = new AssetAuditItem(
                GuidGenerator.Create(),
                id,
                asset.Id,
                asset.Location,
                asset.AssignedToUserId,
                asset.AssignedToUserName
            );
            item.ResultStatus = AuditItemResult.Unexpected;
            isUnexpected = true;
        }

        // Determine scanned location and scanned user
        var scannedLocation = !string.IsNullOrWhiteSpace(input.CurrentLocation)
            ? input.CurrentLocation
            : (!string.IsNullOrWhiteSpace(session.ScopeLocation) ? session.ScopeLocation : asset.Location);

        var scannedUserId = input.CurrentAssignedUserId ?? asset.AssignedToUserId;
        var scannedUserName = input.CurrentAssignedUserName ?? asset.AssignedToUserName;

        if (isUnexpected)
        {
            item.ScannedLocation = scannedLocation;
            item.ScannedAssignedToUserId = scannedUserId;
            item.ScannedAssignedToUserName = scannedUserName;
            item.ScannedByUserId = CurrentUser.Id;
            item.ScannedByUserName = CurrentUser.UserName ?? CurrentUser.Name;
            item.ScannedTime = DateTime.Now;
            item.Notes = input.Notes ?? "Thiết bị ngoài danh mục ban đầu";
            await _itemRepository.InsertAsync(item);
        }
        else
        {
            item.RecordScan(
                scannedLocation,
                scannedUserId,
                scannedUserName,
                CurrentUser.Id,
                CurrentUser.UserName ?? CurrentUser.Name,
                input.Notes
            );
            await _itemRepository.UpdateAsync(item);
        }

        // Recalculate counts
        var allItems = await AsyncExecuter.ToListAsync(itemQuery.Where(x => x.AuditSessionId == id));
        int total = allItems.Count;
        int scanned = allItems.Count(x => x.ResultStatus != AuditItemResult.Pending);
        int matched = allItems.Count(x => x.ResultStatus == AuditItemResult.Matched);
        int displaced = allItems.Count(x => x.ResultStatus == AuditItemResult.Displaced);
        int missing = allItems.Count(x => x.ResultStatus == AuditItemResult.Pending);

        session.UpdateCounts(total, scanned, matched, displaced, missing);
        await _sessionRepository.UpdateAsync(session);

        // Log activity
        await _assetManager.LogAuditScannedAsync(
            asset,
            session,
            item.ResultStatus,
            scannedLocation,
            scannedUserName,
            CurrentUser.Id,
            CurrentUser.UserName ?? CurrentUser.Name
        );

        var countsDto = new AuditSessionCountsDto
        {
            TotalExpectedCount = total,
            ScannedCount = scanned,
            MatchedCount = matched,
            DisplacedCount = displaced,
            MissingCount = missing,
            ProgressPercentage = total > 0 ? Math.Round((double)scanned / total * 100, 1) : 0
        };

        string msg = item.ResultStatus switch
        {
            AuditItemResult.Matched => $"[Khớp hoàn toàn] {asset.Name} ({asset.AssetTag}) đúng vị trí và người quản lý.",
            AuditItemResult.Displaced => $"[Lệch vị trí/người dùng] {asset.Name} ({asset.AssetTag}) đang ở '{scannedLocation ?? "Chưa rõ"}' (Hồ sơ: '{item.ExpectedLocation ?? "Chưa rõ"}').",
            AuditItemResult.Unexpected => $"[Ngoài danh mục] {asset.Name} ({asset.AssetTag}) thuộc phạm vi khác đã được ghi nhận vào đợt kiểm kê này.",
            _ => $"Đã ghi nhận quét {asset.Name}."
        };

        string? displacedReason = null;
        if (item.ResultStatus == AuditItemResult.Displaced)
        {
            var reasons = new List<string>();
            if (!string.Equals(item.ExpectedLocation?.Trim(), scannedLocation?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Vị trí thực tế '{scannedLocation}' khác vị trí hồ sơ '{item.ExpectedLocation}'");
            }
            if (item.ExpectedAssignedToUserId != scannedUserId)
            {
                reasons.Add($"Người dùng thực tế '{scannedUserName}' khác hồ sơ '{item.ExpectedAssignedToUserName}'");
            }
            displacedReason = string.Join(". ", reasons);
        }

        return new ScanResultDto
        {
            Success = true,
            Message = msg,
            Item = MapItemToDto(item, asset),
            ResultStatus = item.ResultStatus,
            IsDisplaced = item.ResultStatus == AuditItemResult.Displaced,
            DisplacedReason = displacedReason,
            UpdatedCounts = countsDto
        };
    }

    public async Task<AssetAuditSessionDto> ReconcileAsync(Guid id, ReconcileAuditItemsInput input)
    {
        var session = await _sessionRepository.GetAsync(id);
        var itemQuery = await _itemRepository.GetQueryableAsync();

        var query = itemQuery.Where(x => x.AuditSessionId == id &&
                                         (x.ResultStatus == AuditItemResult.Displaced || x.ResultStatus == AuditItemResult.Unexpected) &&
                                         !x.IsReconciled);

        if (input.ItemIds != null && input.ItemIds.Any())
        {
            query = query.Where(x => input.ItemIds.Contains(x.Id));
        }

        var itemsToReconcile = await AsyncExecuter.ToListAsync(query);

        foreach (var item in itemsToReconcile)
        {
            var asset = await _assetRepository.GetAsync(item.AssetId);
            var oldLoc = asset.Location;
            var oldUser = asset.AssignedToUserName;

            // Apply scanned location
            if (!string.IsNullOrWhiteSpace(item.ScannedLocation))
            {
                asset.Location = item.ScannedLocation;
            }

            // Apply scanned user if changed
            if (item.ScannedAssignedToUserId.HasValue)
            {
                asset.AssignTo(
                    item.ScannedAssignedToUserId,
                    item.ScannedAssignedToUserName,
                    null,
                    asset.Department
                );
            }

            await _assetRepository.UpdateAsync(asset);

            item.MarkReconciled();
            await _itemRepository.UpdateAsync(item);

            await _assetManager.LogAuditReconciledAsync(
                asset,
                session,
                oldLoc,
                asset.Location,
                oldUser,
                asset.AssignedToUserName,
                CurrentUser.Id,
                CurrentUser.UserName ?? CurrentUser.Name
            );
        }

        return await GetAsync(id);
    }

    public async Task<AssetAuditSessionDto> CompleteAsync(Guid id)
    {
        var session = await _sessionRepository.GetAsync(id);
        session.Complete();
        await _sessionRepository.UpdateAsync(session);
        return await GetAsync(id);
    }

    public async Task<AssetAuditSessionDto> CancelAsync(Guid id, string? reason = null)
    {
        var session = await _sessionRepository.GetAsync(id);
        session.Cancel();
        if (!string.IsNullOrWhiteSpace(reason))
        {
            session.Notes = string.IsNullOrEmpty(session.Notes) ? $"[HỦY]: {reason}" : $"{session.Notes}\n[HỦY]: {reason}";
        }
        await _sessionRepository.UpdateAsync(session);
        return await GetAsync(id);
    }

    public async Task<AuditReportDto> GetReportAsync(Guid id)
    {
        var sessionDto = await GetAsync(id);

        return new AuditReportDto
        {
            Session = sessionDto,
            GeneratedDate = DateTime.Now,
            AuditorName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản trị viên IT",
            MatchedItems = sessionDto.Items.Where(x => x.ResultStatus == AuditItemResult.Matched).ToList(),
            DisplacedItems = sessionDto.Items.Where(x => x.ResultStatus == AuditItemResult.Displaced).ToList(),
            MissingItems = sessionDto.Items.Where(x => x.ResultStatus == AuditItemResult.Pending).ToList(),
            UnexpectedItems = sessionDto.Items.Where(x => x.ResultStatus == AuditItemResult.Unexpected).ToList()
        };
    }

    private static AssetAuditSessionDto MapSessionToDto(AssetAuditSession session)
    {
        double progress = session.TotalExpectedCount > 0
            ? Math.Round((double)session.ScannedCount / session.TotalExpectedCount * 100, 1)
            : 0;

        return new AssetAuditSessionDto
        {
            Id = session.Id,
            Title = session.Title,
            AuditCode = session.AuditCode,
            ScopeDepartment = session.ScopeDepartment,
            ScopeLocation = session.ScopeLocation,
            Status = session.Status,
            StatusName = GetAuditStatusName(session.Status),
            StartDate = session.StartDate,
            CompletedDate = session.CompletedDate,
            TotalExpectedCount = session.TotalExpectedCount,
            ScannedCount = session.ScannedCount,
            MatchedCount = session.MatchedCount,
            DisplacedCount = session.DisplacedCount,
            MissingCount = session.MissingCount,
            ProgressPercentage = progress,
            Notes = session.Notes,
            CreationTime = session.CreationTime,
            CreatorId = session.CreatorId
        };
    }

    private static AssetAuditItemDto MapItemToDto(AssetAuditItem item, Asset? asset)
    {
        return new AssetAuditItemDto
        {
            Id = item.Id,
            AuditSessionId = item.AuditSessionId,
            AssetId = item.AssetId,
            AssetTag = asset?.AssetTag ?? "N/A",
            AssetName = asset?.Name ?? "Thiết bị không xác định",
            AssetTypeName = asset != null ? GetAssetTypeName(asset.AssetType) : "N/A",
            SerialNumber = asset?.SerialNumber,
            Model = asset?.Model,
            ExpectedLocation = item.ExpectedLocation,
            ExpectedAssignedToUserId = item.ExpectedAssignedToUserId,
            ExpectedAssignedToUserName = item.ExpectedAssignedToUserName,
            ScannedLocation = item.ScannedLocation,
            ScannedAssignedToUserId = item.ScannedAssignedToUserId,
            ScannedAssignedToUserName = item.ScannedAssignedToUserName,
            ResultStatus = item.ResultStatus,
            ResultStatusName = GetAuditItemResultName(item.ResultStatus),
            ScannedTime = item.ScannedTime,
            ScannedByUserId = item.ScannedByUserId,
            ScannedByUserName = item.ScannedByUserName,
            Notes = item.Notes,
            IsReconciled = item.IsReconciled,
            ReconciledTime = item.ReconciledTime,
            CreationTime = item.CreationTime,
            CreatorId = item.CreatorId
        };
    }

    private static string GetAuditStatusName(AssetAuditStatus status)
    {
        return status switch
        {
            AssetAuditStatus.Draft => "Dự thảo",
            AssetAuditStatus.InProgress => "Đang kiểm kê",
            AssetAuditStatus.Completed => "Đã hoàn tất",
            AssetAuditStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    private static string GetAuditItemResultName(AuditItemResult result)
    {
        return result switch
        {
            AuditItemResult.Pending => "Chưa quét",
            AuditItemResult.Matched => "Khớp hoàn toàn",
            AuditItemResult.Displaced => "Lệch vị trí / Người dùng",
            AuditItemResult.Unexpected => "Ngoài danh mục",
            _ => result.ToString()
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
            AssetType.PrinterPeripheral => "Máy in & Ngoại vi",
            AssetType.ServerStorage => "Máy chủ & Lưu trữ",
            AssetType.SoftwareLicense => "Bản quyền",
            AssetType.MobileDevice => "Thiết bị di động",
            _ => "Khác"
        };
    }
}
