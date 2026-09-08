using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.TicketStatuses;

public interface ITicketStatusAppService : ICrudAppService<
    TicketStatusDto,
    Guid,
    TicketStatusGetListInput,
    CreateUpdateTicketStatusDto>
{
}
