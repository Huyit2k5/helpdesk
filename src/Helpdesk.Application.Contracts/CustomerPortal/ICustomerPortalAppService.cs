using System;
using System.Threading.Tasks;
using Helpdesk.CustomerPortal.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.CustomerPortal;

public interface ICustomerPortalAppService : IApplicationService
{
    Task<PagedResultDto<CustomerTicketDto>> GetMyTicketsAsync(GetCustomerTicketListInput input);

    Task<CustomerTicketDetailDto> GetMyTicketAsync(Guid id);

    Task<CustomerTicketDetailDto> CreateMyTicketAsync(CreateCustomerTicketDto input);

    Task<CustomerCommentDto> AddMyCommentAsync(Guid ticketId, AddCustomerCommentDto input);
}
