using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Helpdesk.Tickets.Dtos;

namespace Helpdesk.CustomerPortal.Dtos;

public class AddCustomerCommentDto
{
    [Required]
    public string Content { get; set; } = null!;

    public List<CreateAttachmentInput>? Attachments { get; set; }
}
