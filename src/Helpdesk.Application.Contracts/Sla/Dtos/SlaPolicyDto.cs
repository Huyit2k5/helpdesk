using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Sla.Dtos;

public class SlaPolicyDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public List<SlaPolicyRuleDto> Rules { get; set; } = new();
}
