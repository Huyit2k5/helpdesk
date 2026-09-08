using Volo.Abp.Application.Dtos;

namespace Helpdesk.TicketSources;

public class TicketSourceGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
