using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Sla.Dtos;

public class SlaBreachLogDto : CreationAuditedEntityDto<Guid>
{
    public Guid TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string TicketTitle { get; set; } = string.Empty;
    public SlaBreachType BreachType { get; set; }
    public string BreachTypeName => BreachType.ToString();
    public DateTime ExpectedDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public double BreachedMinutes { get; set; }
    public string? Reason { get; set; }
}

public class GetSlaBreachListInput : PagedAndSortedResultRequestDto
{
    public SlaBreachType? BreachType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
