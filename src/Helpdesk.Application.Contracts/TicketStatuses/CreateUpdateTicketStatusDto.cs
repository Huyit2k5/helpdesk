using System.ComponentModel.DataAnnotations;
using Helpdesk.Categories;

namespace Helpdesk.TicketStatuses;

public class CreateUpdateTicketStatusDto
{
    [Required]
    [StringLength(TicketStatusConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(TicketStatusConsts.MaxCodeLength)]
    public string Code { get; set; } = null!;

    [StringLength(TicketStatusConsts.MaxColorLength)]
    public string? Color { get; set; }

    public bool IsFinal { get; set; }
    public bool IsDefault { get; set; }
    public StatusGroup StatusGroup { get; set; }
    public int Order { get; set; }
}
