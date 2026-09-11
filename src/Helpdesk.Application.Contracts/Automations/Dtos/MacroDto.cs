using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Automations.Dtos;

public class MacroDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public List<RuleAction> Actions { get; set; } = new();
}

public class CreateUpdateMacroDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public List<RuleAction> Actions { get; set; } = new();
}

public class GetMacroListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}
