using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Departments;

public class DepartmentDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
