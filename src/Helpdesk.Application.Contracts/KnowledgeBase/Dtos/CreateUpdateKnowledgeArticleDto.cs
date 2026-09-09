using System;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.KnowledgeBase.Dtos;

public class CreateUpdateKnowledgeArticleDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; } = null!;

    [StringLength(256)]
    public string? Slug { get; set; }

    [Required]
    public Guid CategoryId { get; set; }

    [StringLength(1000)]
    public string? Summary { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    [StringLength(500)]
    public string? Tags { get; set; }

    public bool IsPublished { get; set; } = true;
}
