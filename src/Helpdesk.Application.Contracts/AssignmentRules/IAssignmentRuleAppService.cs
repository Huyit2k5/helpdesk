using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.AssignmentRules.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.AssignmentRules;

public interface IAssignmentRuleAppService : IApplicationService
{
    Task<PagedResultDto<AssignmentRuleDto>> GetListAsync(AssignmentRuleGetListInput input);

    Task<AssignmentRuleDto> GetAsync(Guid id);

    Task<AssignmentRuleDto> CreateAsync(CreateUpdateAssignmentRuleDto input);

    Task<AssignmentRuleDto> UpdateAsync(Guid id, CreateUpdateAssignmentRuleDto input);

    Task DeleteAsync(Guid id);

    Task<AssignmentRuleDto> ToggleActiveAsync(Guid id);

    Task<List<AgentLookupDto>> GetAgentLookupAsync();
}
