using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Sla.Dtos;

public class CreateUpdateSlaPolicyDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public List<CreateUpdateSlaPolicyRuleDto> Rules { get; set; } = new();
}
