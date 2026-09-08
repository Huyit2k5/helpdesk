using Volo.Abp.Application.Dtos;

namespace Helpdesk.Departments;

public class DepartmentGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
