using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.CustomerPortal.Dtos;

public class GetCustomerTicketListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsClosed { get; set; }
}
