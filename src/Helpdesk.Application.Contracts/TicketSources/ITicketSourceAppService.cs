using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.TicketSources;

public interface ITicketSourceAppService : ICrudAppService<
    TicketSourceDto,
    Guid,
    TicketSourceGetListInput,
    CreateUpdateTicketSourceDto>
{
}
