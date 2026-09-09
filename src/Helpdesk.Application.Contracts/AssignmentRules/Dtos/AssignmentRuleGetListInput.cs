using Volo.Abp.Application.Dtos;

namespace Helpdesk.AssignmentRules.Dtos;

public class AssignmentRuleGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
