using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Assets.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Assets;

public interface IAssetAppService : IApplicationService
{
    Task<PagedResultDto<AssetDto>> GetListAsync(GetAssetsInput input);

    Task<AssetDetailDto> GetAsync(Guid id);

    Task<AssetDto> CreateAsync(CreateAssetDto input);

    Task<AssetDto> UpdateAsync(Guid id, UpdateAssetDto input);

    Task DeleteAsync(Guid id);

    Task AssignAsync(Guid id, AssignAssetDto input);

    Task ReturnAsync(Guid id, ReturnAssetDto? input);

    Task ChangeStatusAsync(Guid id, ChangeAssetStatusDto input);

    Task<AssetKpiDto> GetKpisAsync();

    Task<List<AssetDto>> GetLookupAsync();

    Task<List<AssetDto>> GetMyAssetsAsync();

    Task<AssetReceiptDto> GetReceiptAsync(Guid id, string? type = "handover");

    Task<AssetDto?> GetByAssetTagAsync(string assetTag);
}
