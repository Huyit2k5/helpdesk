using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Categories;

public class CategoryGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? ParentId { get; set; }
    public bool? IsActive { get; set; }
}
