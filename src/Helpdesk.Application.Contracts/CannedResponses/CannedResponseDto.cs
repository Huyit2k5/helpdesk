using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.CannedResponses;

public class CannedResponseDto : FullAuditedEntityDto<Guid>
{
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsPublic { get; set; }
    public int UsageCount { get; set; }
}
