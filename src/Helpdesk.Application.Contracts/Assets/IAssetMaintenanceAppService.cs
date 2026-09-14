using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Assets.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Assets;

public interface IAssetMaintenanceAppService : IApplicationService
{
    Task<PagedResultDto<AssetMaintenanceDto>> GetListAsync(GetAssetMaintenanceListInput input);

    Task<AssetMaintenanceDto> GetAsync(Guid id);

    Task<List<AssetMaintenanceDto>> GetByAssetIdAsync(Guid assetId);

    Task<AssetMaintenanceDto> CreateAsync(CreateAssetMaintenanceDto input);

    Task<AssetMaintenanceDto> CompleteAsync(Guid id, CompleteAssetMaintenanceDto input);

    Task<AssetMaintenanceDto> CancelAsync(Guid id, string? reason);

    Task<AssetTcoSummaryDto> GetTcoSummaryAsync(Guid assetId);

    Task<List<MaintenanceScheduleAlertDto>> GetScheduleAlertsAsync(int daysThreshold = 30);
}
