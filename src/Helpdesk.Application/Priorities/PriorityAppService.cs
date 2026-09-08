using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Priorities;

[Authorize(HelpdeskPermissions.Priorities.Default)]
public class PriorityAppService : CrudAppService<
    Priority,
    PriorityDto,
    Guid,
    PriorityGetListInput,
    CreateUpdatePriorityDto>,
    IPriorityAppService
{
    private readonly IPriorityRepository _priorityRepository;
    private readonly PriorityManager _priorityManager;

    public PriorityAppService(
        IPriorityRepository priorityRepository,
        PriorityManager priorityManager)
        : base(priorityRepository)
    {
        _priorityRepository = priorityRepository;
        _priorityManager = priorityManager;

        GetPolicyName = HelpdeskPermissions.Priorities.Default;
        GetListPolicyName = HelpdeskPermissions.Priorities.Default;
        CreatePolicyName = HelpdeskPermissions.Priorities.Create;
        UpdatePolicyName = HelpdeskPermissions.Priorities.Edit;
        DeletePolicyName = HelpdeskPermissions.Priorities.Delete;
    }

    [Authorize(HelpdeskPermissions.Priorities.Create)]
    public override async Task<PriorityDto> CreateAsync(CreateUpdatePriorityDto input)
    {
        var entity = await _priorityManager.CreateAsync(
            input.Name, input.Code, input.Color,
            input.SlaResponseHours, input.SlaResolutionHours,
            input.Order, input.IsActive);

        await _priorityRepository.InsertAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    [Authorize(HelpdeskPermissions.Priorities.Edit)]
    public override async Task<PriorityDto> UpdateAsync(Guid id, CreateUpdatePriorityDto input)
    {
        var entity = await _priorityRepository.GetAsync(id);

        entity.SetName(input.Name);
        await _priorityManager.ChangeCodeAsync(entity, input.Code);
        entity.Color = input.Color;
        entity.SlaResponseHours = input.SlaResponseHours;
        entity.SlaResolutionHours = input.SlaResolutionHours;
        entity.Order = input.Order;
        entity.IsActive = input.IsActive;

        await _priorityRepository.UpdateAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    protected override async Task<IQueryable<Priority>> CreateFilteredQueryAsync(PriorityGetListInput input)
    {
        var queryable = await base.CreateFilteredQueryAsync(input);

        return queryable
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Filter!) || x.Code.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue,
                x => x.IsActive == input.IsActive!.Value);
    }

    protected override Task<PriorityDto> MapToGetOutputDtoAsync(Priority entity) =>
        Task.FromResult(MapToDto(entity));

    protected override Task<PriorityDto> MapToGetListOutputDtoAsync(Priority entity) =>
        Task.FromResult(MapToDto(entity));

    private static PriorityDto MapToDto(Priority entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        Color = entity.Color,
        SlaResponseHours = entity.SlaResponseHours,
        SlaResolutionHours = entity.SlaResolutionHours,
        Order = entity.Order,
        IsActive = entity.IsActive,
        CreationTime = entity.CreationTime,
        CreatorId = entity.CreatorId,
        LastModificationTime = entity.LastModificationTime,
        LastModifierId = entity.LastModifierId
    };
}
