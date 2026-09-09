using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Helpdesk.KnowledgeBase;

/// <summary>
/// Bài viết cẩm nang kỹ thuật / Cơ sở tri thức (Knowledge Base).
/// </summary>
public class KnowledgeArticle : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Title { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public Guid CategoryId { get; private set; }

    public string? Summary { get; private set; }

    public string Content { get; private set; } = null!;

    public string? Tags { get; private set; }

    public bool IsPublished { get; private set; }

    public int ViewCount { get; private set; }

    public int HelpfulCount { get; private set; }

    public int NotHelpfulCount { get; private set; }

    protected KnowledgeArticle()
    {
        // For EF Core
    }

    public KnowledgeArticle(
        Guid id,
        string title,
        string slug,
        Guid categoryId,
        string content,
        string? summary = null,
        string? tags = null,
        bool isPublished = true)
        : base(id)
    {
        SetTitle(title, slug);
        CategoryId = categoryId;
        SetContent(content, summary);
        Tags = tags;
        IsPublished = isPublished;
        ViewCount = 0;
        HelpfulCount = 0;
        NotHelpfulCount = 0;
    }

    public void SetTitle(string title, string slug)
    {
        Title = Check.NotNullOrWhiteSpace(title, nameof(title), 256);
        Slug = Check.NotNullOrWhiteSpace(slug, nameof(slug), 256);
    }

    public void SetCategory(Guid categoryId)
    {
        CategoryId = categoryId;
    }

    public void SetContent(string content, string? summary = null)
    {
        Content = Check.NotNullOrWhiteSpace(content, nameof(content));
        Summary = summary;
    }

    public void SetTags(string? tags)
    {
        Tags = tags;
    }

    public void SetPublished(bool isPublished)
    {
        IsPublished = isPublished;
    }

    public void IncrementViewCount()
    {
        ViewCount++;
    }

    public void Vote(bool isHelpful)
    {
        if (isHelpful)
        {
            HelpfulCount++;
        }
        else
        {
            NotHelpfulCount++;
        }
    }
}
