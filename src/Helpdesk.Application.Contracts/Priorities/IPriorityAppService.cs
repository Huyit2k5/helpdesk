using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.Priorities;

public interface IPriorityAppService : ICrudAppService<
    PriorityDto,
    Guid,
    PriorityGetListInput,
    CreateUpdatePriorityDto>
{
}
