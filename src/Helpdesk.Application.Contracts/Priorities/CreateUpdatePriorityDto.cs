using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Priorities;

public class CreateUpdatePriorityDto
{
    [Required]
    [StringLength(PriorityConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(PriorityConsts.MaxCodeLength)]
    public string Code { get; set; } = null!;

    [StringLength(PriorityConsts.MaxColorLength)]
    public string? Color { get; set; }

    public int SlaResponseHours { get; set; }
    public int SlaResolutionHours { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
