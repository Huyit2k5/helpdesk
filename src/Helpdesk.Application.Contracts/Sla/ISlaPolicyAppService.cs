using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.Sla.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Sla;

public interface ISlaPolicyAppService : IApplicationService
{
    Task<PagedResultDto<SlaPolicyDto>> GetListAsync(GetSlaPolicyListInput input);
    Task<SlaPolicyDto> GetAsync(Guid id);
    Task<SlaPolicyDto> CreateAsync(CreateUpdateSlaPolicyDto input);
    Task<SlaPolicyDto> UpdateAsync(Guid id, CreateUpdateSlaPolicyDto input);
    Task DeleteAsync(Guid id);
    Task<List<SlaPolicyDto>> GetActivePolicyListAsync();
}
