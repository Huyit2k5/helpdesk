using System;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Tickets.Dtos;

public class AssignTicketInput
{
    public Guid? AssigneeId { get; set; }

    public Guid? DepartmentId { get; set; }
}

public class ChangeTicketStatusInput
{
    [Required]
    public Guid StatusId { get; set; }

    public string? Comment { get; set; }
}
