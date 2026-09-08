using System;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.CannedResponses;

public class CreateUpdateCannedResponseDto
{
    [Required]
    [StringLength(CannedResponseConsts.MaxTitleLength)]
    public string Title { get; set; } = null!;

    [Required]
    [StringLength(CannedResponseConsts.MaxContentLength)]
    public string Content { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public bool IsPublic { get; set; } = true;
}
