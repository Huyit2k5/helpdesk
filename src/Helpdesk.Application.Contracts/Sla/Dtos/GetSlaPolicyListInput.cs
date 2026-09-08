using Volo.Abp.Application.Dtos;

namespace Helpdesk.Sla.Dtos;

public class GetSlaPolicyListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
