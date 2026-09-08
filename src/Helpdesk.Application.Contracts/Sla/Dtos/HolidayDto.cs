using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Sla.Dtos;

public class HolidayDto : EntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public DateTime Date { get; set; }
    public bool IsRecurring { get; set; }
}

public class CreateUpdateHolidayDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; }

    public bool IsRecurring { get; set; }
}
