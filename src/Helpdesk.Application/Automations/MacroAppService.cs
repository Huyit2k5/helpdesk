using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Helpdesk.Automations.Dtos;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.Automations;

[Authorize]
public class MacroAppService : ApplicationService, IMacroAppService
{
    private readonly IRepository<Macro, Guid> _macroRepository;

    public MacroAppService(IRepository<Macro, Guid> macroRepository)
    {
        _macroRepository = macroRepository;
    }

    [Authorize(HelpdeskPermissions.Macros.Default)]
    public async Task<PagedResultDto<MacroDto>> GetListAsync(GetMacroListInput input)
    {
        var query = await _macroRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(filter) || (m.Description != null && m.Description.ToLower().Contains(filter)));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(m => m.IsActive == input.IsActive.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query.OrderBy(m => m.Order).PageBy(input)
        );

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResultDto<MacroDto>(totalCount, dtos);
    }

    /// <summary>
    /// Lấy danh sách Macro đang hoạt động để kỹ thuật viên chọn nhanh trên chi tiết vé.
    /// </summary>
    [Authorize(HelpdeskPermissions.Tickets.Default)]
    public async Task<List<MacroDto>> GetActiveMacrosAsync()
    {
        var items = await _macroRepository.GetListAsync(m => m.IsActive);
        return items.OrderBy(m => m.Order).Select(MapToDto).ToList();
    }

    [Authorize(HelpdeskPermissions.Macros.Default)]
    public async Task<MacroDto> GetAsync(Guid id)
    {
        var macro = await _macroRepository.GetAsync(id);
        return MapToDto(macro);
    }

    [Authorize(HelpdeskPermissions.Macros.Create)]
    public async Task<MacroDto> CreateAsync(CreateUpdateMacroDto input)
    {
        var macro = new Macro(
            GuidGenerator.Create(),
            input.Name,
            input.Description,
            input.Order,
            input.IsActive
        );

        macro.SetActions(input.Actions);

        await _macroRepository.InsertAsync(macro, autoSave: true);
        return MapToDto(macro);
    }

    [Authorize(HelpdeskPermissions.Macros.Edit)]
    public async Task<MacroDto> UpdateAsync(Guid id, CreateUpdateMacroDto input)
    {
        var macro = await _macroRepository.GetAsync(id);

        macro.SetName(input.Name);
        macro.Description = input.Description;
        macro.Order = input.Order;
        macro.IsActive = input.IsActive;

        macro.SetActions(input.Actions);

        await _macroRepository.UpdateAsync(macro, autoSave: true);
        return MapToDto(macro);
    }

    [Authorize(HelpdeskPermissions.Macros.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _macroRepository.DeleteAsync(id);
    }

    [Authorize(HelpdeskPermissions.Macros.Edit)]
    public async Task<MacroDto> ToggleActiveAsync(Guid id)
    {
        var macro = await _macroRepository.GetAsync(id);
        macro.IsActive = !macro.IsActive;
        await _macroRepository.UpdateAsync(macro, autoSave: true);
        return MapToDto(macro);
    }

    private static MacroDto MapToDto(Macro macro)
    {
        return new MacroDto
        {
            Id = macro.Id,
            CreationTime = macro.CreationTime,
            CreatorId = macro.CreatorId,
            LastModificationTime = macro.LastModificationTime,
            LastModifierId = macro.LastModifierId,
            Name = macro.Name,
            Description = macro.Description,
            Order = macro.Order,
            IsActive = macro.IsActive,
            Actions = macro.GetActions()
        };
    }
}
