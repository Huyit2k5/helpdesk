using Volo.Abp.Application.Dtos;

namespace Helpdesk.TicketStatuses;

public class TicketStatusGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
