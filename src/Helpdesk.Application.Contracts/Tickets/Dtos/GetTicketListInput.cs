using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.Tickets.Dtos;

public class GetTicketListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public Guid? StatusId { get; set; }

    public Guid? PriorityId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? DepartmentId { get; set; }

    public Guid? AssigneeId { get; set; }

    public bool AssignedToMe { get; set; }

    public Guid? SourceId { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}
