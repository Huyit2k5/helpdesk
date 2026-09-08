using Volo.Abp.Application.Dtos;

namespace Helpdesk.Priorities;

public class PriorityGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
