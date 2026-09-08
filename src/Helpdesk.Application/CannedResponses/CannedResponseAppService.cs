using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.CannedResponses;

[Authorize(HelpdeskPermissions.CannedResponses.Default)]
public class CannedResponseAppService : CrudAppService<
    CannedResponse,
    CannedResponseDto,
    Guid,
    CannedResponseGetListInput,
    CreateUpdateCannedResponseDto>,
    ICannedResponseAppService
{
    private readonly ICannedResponseRepository _cannedResponseRepository;

    public CannedResponseAppService(
        ICannedResponseRepository cannedResponseRepository)
        : base(cannedResponseRepository)
    {
        _cannedResponseRepository = cannedResponseRepository;

        GetPolicyName = HelpdeskPermissions.CannedResponses.Default;
        GetListPolicyName = HelpdeskPermissions.CannedResponses.Default;
        CreatePolicyName = HelpdeskPermissions.CannedResponses.Create;
        UpdatePolicyName = HelpdeskPermissions.CannedResponses.Edit;
        DeletePolicyName = HelpdeskPermissions.CannedResponses.Delete;
    }

    [Authorize(HelpdeskPermissions.CannedResponses.Create)]
    public override async Task<CannedResponseDto> CreateAsync(CreateUpdateCannedResponseDto input)
    {
        var entity = new CannedResponse(
            GuidGenerator.Create(),
            input.Title,
            input.Content,
            input.CategoryId,
            input.IsPublic);

        await _cannedResponseRepository.InsertAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    [Authorize(HelpdeskPermissions.CannedResponses.Edit)]
    public override async Task<CannedResponseDto> UpdateAsync(Guid id, CreateUpdateCannedResponseDto input)
    {
        var entity = await _cannedResponseRepository.GetAsync(id);

        entity.SetTitle(input.Title);
        entity.Content = input.Content;
        entity.CategoryId = input.CategoryId;
        entity.IsPublic = input.IsPublic;

        await _cannedResponseRepository.UpdateAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    protected override async Task<IQueryable<CannedResponse>> CreateFilteredQueryAsync(CannedResponseGetListInput input)
    {
        var queryable = await base.CreateFilteredQueryAsync(input);

        return queryable
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                x => x.Title.Contains(input.Filter!) || x.Content.Contains(input.Filter!))
            .WhereIf(input.CategoryId.HasValue,
                x => x.CategoryId == input.CategoryId)
            .WhereIf(input.IsPublic.HasValue,
                x => x.IsPublic == input.IsPublic!.Value);
    }

    protected override Task<CannedResponseDto> MapToGetOutputDtoAsync(CannedResponse entity) =>
        Task.FromResult(MapToDto(entity));

    protected override Task<CannedResponseDto> MapToGetListOutputDtoAsync(CannedResponse entity) =>
        Task.FromResult(MapToDto(entity));

    private static CannedResponseDto MapToDto(CannedResponse entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Content = entity.Content,
        CategoryId = entity.CategoryId,
        IsPublic = entity.IsPublic,
        UsageCount = entity.UsageCount,
        CreationTime = entity.CreationTime,
        CreatorId = entity.CreatorId,
        LastModificationTime = entity.LastModificationTime,
        LastModifierId = entity.LastModifierId
    };
}
