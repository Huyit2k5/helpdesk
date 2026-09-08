using System;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Categories;

public class CreateUpdateCategoryDto
{
    [Required]
    [StringLength(CategoryConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(CategoryConsts.MaxCodeLength)]
    public string Code { get; set; } = null!;

    public Guid? ParentId { get; set; }

    [StringLength(CategoryConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int Order { get; set; }
}
