using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Categories;

public class CategoryDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}
