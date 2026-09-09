using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Tickets.Dtos;

public class CreateAttachmentInput
{
    [Required]
    [StringLength(256)]
    public string FileName { get; set; } = null!;

    public long FileSize { get; set; }

    [Required]
    [StringLength(128)]
    public string ContentType { get; set; } = null!;

    [Required]
    public string BlobName { get; set; } = null!;
}
