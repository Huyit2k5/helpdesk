using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Automations.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Automations;

public interface IAutomationRuleAppService : IApplicationService
{
    Task<PagedResultDto<AutomationRuleDto>> GetListAsync(GetAutomationRuleListInput input);
    Task<AutomationRuleDto> GetAsync(Guid id);
    Task<AutomationRuleDto> CreateAsync(CreateUpdateAutomationRuleDto input);
    Task<AutomationRuleDto> UpdateAsync(Guid id, CreateUpdateAutomationRuleDto input);
    Task DeleteAsync(Guid id);
    Task<AutomationRuleDto> ToggleActiveAsync(Guid id);
}

public interface IMacroAppService : IApplicationService
{
    Task<PagedResultDto<MacroDto>> GetListAsync(GetMacroListInput input);
    Task<List<MacroDto>> GetActiveMacrosAsync();
    Task<MacroDto> GetAsync(Guid id);
    Task<MacroDto> CreateAsync(CreateUpdateMacroDto input);
    Task<MacroDto> UpdateAsync(Guid id, CreateUpdateMacroDto input);
    Task DeleteAsync(Guid id);
    Task<MacroDto> ToggleActiveAsync(Guid id);
}
