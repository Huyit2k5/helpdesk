using System;
using System.Threading.Tasks;
using Helpdesk.Assets.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Assets;

public interface IAssetAuditAppService : IApplicationService
{
    Task<PagedResultDto<AssetAuditSessionDto>> GetListAsync(GetAssetAuditListInput input);

    Task<AssetAuditSessionDto> GetAsync(Guid id);

    Task<AssetAuditSessionDto> CreateAsync(CreateAssetAuditSessionDto input);

    Task<AssetAuditSessionDto> StartAsync(Guid id);

    Task<ScanResultDto> ScanItemAsync(Guid id, ScanAuditItemInput input);

    Task<AssetAuditSessionDto> ReconcileAsync(Guid id, ReconcileAuditItemsInput input);

    Task<AssetAuditSessionDto> CompleteAsync(Guid id);

    Task<AssetAuditSessionDto> CancelAsync(Guid id, string? reason = null);

    Task<AuditReportDto> GetReportAsync(Guid id);
}
