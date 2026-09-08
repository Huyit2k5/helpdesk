using System;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.TicketStatuses;

[Authorize(HelpdeskPermissions.TicketStatuses.Default)]
public class TicketStatusAppService : CrudAppService<
    TicketStatus,
    TicketStatusDto,
    Guid,
    TicketStatusGetListInput,
    CreateUpdateTicketStatusDto>,
    ITicketStatusAppService
{
    private readonly ITicketStatusRepository _ticketStatusRepository;
    private readonly TicketStatusManager _ticketStatusManager;

    public TicketStatusAppService(
        ITicketStatusRepository ticketStatusRepository,
        TicketStatusManager ticketStatusManager)
        : base(ticketStatusRepository)
    {
        _ticketStatusRepository = ticketStatusRepository;
        _ticketStatusManager = ticketStatusManager;

        GetPolicyName = HelpdeskPermissions.TicketStatuses.Default;
        GetListPolicyName = HelpdeskPermissions.TicketStatuses.Default;
        CreatePolicyName = HelpdeskPermissions.TicketStatuses.Create;
        UpdatePolicyName = HelpdeskPermissions.TicketStatuses.Edit;
        DeletePolicyName = HelpdeskPermissions.TicketStatuses.Delete;
    }

    [Authorize(HelpdeskPermissions.TicketStatuses.Create)]
    public override async Task<TicketStatusDto> CreateAsync(CreateUpdateTicketStatusDto input)
    {
        var entity = await _ticketStatusManager.CreateAsync(
            input.Name, input.Code, input.StatusGroup,
            input.Color, input.IsFinal, input.IsDefault, input.Order);

        await _ticketStatusRepository.InsertAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    [Authorize(HelpdeskPermissions.TicketStatuses.Edit)]
    public override async Task<TicketStatusDto> UpdateAsync(Guid id, CreateUpdateTicketStatusDto input)
    {
        var entity = await _ticketStatusRepository.GetAsync(id);

        entity.SetName(input.Name);
        await _ticketStatusManager.ChangeCodeAsync(entity, input.Code);
        entity.Color = input.Color;
        entity.IsFinal = input.IsFinal;
        entity.IsDefault = input.IsDefault;
        entity.StatusGroup = input.StatusGroup;
        entity.Order = input.Order;

        await _ticketStatusRepository.UpdateAsync(entity, autoSave: true);
        return MapToDto(entity);
    }

    protected override async Task<IQueryable<TicketStatus>> CreateFilteredQueryAsync(TicketStatusGetListInput input)
    {
        var queryable = await base.CreateFilteredQueryAsync(input);

        return queryable
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.Filter!) || x.Code.Contains(input.Filter!));
    }

    protected override Task<TicketStatusDto> MapToGetOutputDtoAsync(TicketStatus entity) =>
        Task.FromResult(MapToDto(entity));

    protected override Task<TicketStatusDto> MapToGetListOutputDtoAsync(TicketStatus entity) =>
        Task.FromResult(MapToDto(entity));

    private static TicketStatusDto MapToDto(TicketStatus entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        Color = entity.Color,
        IsFinal = entity.IsFinal,
        IsDefault = entity.IsDefault,
        StatusGroup = entity.StatusGroup,
        Order = entity.Order,
        CreationTime = entity.CreationTime,
        CreatorId = entity.CreatorId,
        LastModificationTime = entity.LastModificationTime,
        LastModifierId = entity.LastModifierId
    };
}
