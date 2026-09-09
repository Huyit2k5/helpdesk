using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Helpdesk.Tickets.Dtos;

namespace Helpdesk.CustomerPortal.Dtos;

public class CreateCustomerTicketDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public Guid CategoryId { get; set; }

    public Guid? PriorityId { get; set; }

    public List<CreateAttachmentInput>? Attachments { get; set; }
}
