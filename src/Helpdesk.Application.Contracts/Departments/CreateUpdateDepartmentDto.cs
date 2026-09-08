using System;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Departments;

public class CreateUpdateDepartmentDto
{
    [Required]
    [StringLength(DepartmentConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(DepartmentConsts.MaxCodeLength)]
    public string Code { get; set; } = null!;

    public Guid? ManagerId { get; set; }

    [StringLength(DepartmentConsts.MaxDescriptionLength)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
