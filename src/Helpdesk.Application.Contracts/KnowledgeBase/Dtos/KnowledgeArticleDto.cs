using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.KnowledgeBase.Dtos;

public class KnowledgeArticleDto : FullAuditedEntityDto<Guid>
{
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? Summary { get; set; }
    public string Content { get; set; } = null!;
    public string? Tags { get; set; }
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
}
