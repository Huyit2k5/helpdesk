using System.ComponentModel.DataAnnotations;

namespace Helpdesk.TicketSources;

public class CreateUpdateTicketSourceDto
{
    [Required]
    [StringLength(TicketSourceConsts.MaxNameLength)]
    public string Name { get; set; } = null!;

    [Required]
    [StringLength(TicketSourceConsts.MaxCodeLength)]
    public string Code { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}
