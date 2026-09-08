using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.TicketSources;

[Authorize(HelpdeskPermissions.TicketSources.Default)]
public class TicketSourceAppService : CrudAppService<
    TicketSource,
    TicketSourceDto,
    Guid,
    TicketSourceGetListInput,
    CreateUpdateTicketSourceDto>,
    ITicketSourceAppService
{
    private readonly ITicketSourceRepository _ticketSourceRepository;
    private readonly TicketSourceManager _ticketSourceManager;

    public TicketSourceAppService(
        ITicketSourceRepository ticketSourceRepository,
        TicketSourceManager ticketSourceManager)
        : base(ticketSourceRepository)
    {
        _ticketSourceRepository = ticketSourceRepository;
        _ticketSourceManager = ticketSourceManager;

        GetPolicyName = HelpdeskPermissions.TicketSources.Default;
        GetListPolicyName = HelpdeskPermissions.TicketSources.Default;
        CreatePolicyName = HelpdeskPermissions.TicketSources.Create;
        UpdatePolicyName = HelpdeskPermissions.TicketSources.Edit;
        DeletePolicyName = HelpdeskPermissions.TicketSources.Delete;
    }

    [Authorize(HelpdeskPermissions.TicketSources.Create)]
    public override async Task<TicketSourceDto> CreateAsync(CreateUpdateTicketSourceDto input)
    {
        var entity = await _ticketSourceManager.CreateAsync(
            input.Name, input.Code, input.IsActive);

        await _ticketSourceRepository.InsertAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    [Authorize(HelpdeskPermissions.TicketSources.Edit)]
    public override async Task<TicketSourceDto> UpdateAsync(Guid id, CreateUpdateTicketSourceDto input)
    {
        var entity = await _ticketSourceRepository.GetAsync(id);

        entity.SetName(input.Name);
        await _ticketSourceManager.ChangeCodeAsync(entity, input.Code);
        entity.IsActive = input.IsActive;

        await _ticketSourceRepository.UpdateAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    protected override async Task<IQueryable<TicketSource>> CreateFilteredQueryAsync(TicketSourceGetListInput input)
    {
        var queryable = await base.CreateFilteredQueryAsync(input);

        return queryable
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Filter!) || x.Code.Contains(input.Filter!))
            .WhereIf(input.IsActive.HasValue,
                x => x.IsActive == input.IsActive!.Value);
    }

    protected override Task<TicketSourceDto> MapToGetOutputDtoAsync(TicketSource entity) =>
        Task.FromResult(MapToDto(entity));

    protected override Task<TicketSourceDto> MapToGetListOutputDtoAsync(TicketSource entity) =>
        Task.FromResult(MapToDto(entity));

    private static TicketSourceDto MapToDto(TicketSource entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        IsActive = entity.IsActive,
        CreationTime = entity.CreationTime,
        CreatorId = entity.CreatorId,
        LastModificationTime = entity.LastModificationTime,
        LastModifierId = entity.LastModifierId
    };
}
