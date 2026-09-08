using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.TicketSources;

public class TicketSourceDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public bool IsActive { get; set; }
}
