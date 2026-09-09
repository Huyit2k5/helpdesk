using System;

namespace Helpdesk.KnowledgeBase.Dtos;

public class KnowledgeArticleSuggestionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Summary { get; set; }
    public int HelpfulCount { get; set; }
}
