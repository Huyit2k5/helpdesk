using System;
using Volo.Abp.Application.Dtos;

namespace Helpdesk.KnowledgeBase.Dtos;

public class GetKnowledgeArticleListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsPublished { get; set; }
    public string? Tag { get; set; }
}
