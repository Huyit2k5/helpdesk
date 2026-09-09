using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Helpdesk.Categories;
using Helpdesk.KnowledgeBase.Dtos;
using Helpdesk.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Helpdesk.KnowledgeBase;

public class KnowledgeArticleAppService : ApplicationService, IKnowledgeArticleAppService
{
    private readonly IRepository<KnowledgeArticle, Guid> _articleRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;

    public KnowledgeArticleAppService(
        IRepository<KnowledgeArticle, Guid> articleRepository,
        IRepository<Category, Guid> categoryRepository)
    {
        _articleRepository = articleRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<PagedResultDto<KnowledgeArticleDto>> GetListAsync(GetKnowledgeArticleListInput input)
    {
        var isManager = await AuthorizationService.IsGrantedAsync(HelpdeskPermissions.KnowledgeBase.Manage);

        var articleQuery = await _articleRepository.GetQueryableAsync();
        var categoryQuery = await _categoryRepository.GetQueryableAsync();

        var query = from a in articleQuery
                    join c in categoryQuery on a.CategoryId equals c.Id into cats
                    from cat in cats.DefaultIfEmpty()
                    select new { Article = a, CategoryName = cat != null ? cat.Name : null };

        if (!isManager)
        {
            query = query.Where(x => x.Article.IsPublished);
        }
        else if (input.IsPublished.HasValue)
        {
            query = query.Where(x => x.Article.IsPublished == input.IsPublished.Value);
        }

        if (input.CategoryId.HasValue)
        {
            query = query.Where(x => x.Article.CategoryId == input.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Tag))
        {
            var tagPattern = input.Tag.Trim();
            query = query.Where(x => x.Article.Tags != null && x.Article.Tags.Contains(tagPattern));
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(x =>
                x.Article.Title.ToLower().Contains(filter) ||
                (x.Article.Summary != null && x.Article.Summary.ToLower().Contains(filter)) ||
                (x.Article.Tags != null && x.Article.Tags.ToLower().Contains(filter)));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var pagedQuery = query
            .OrderByDescending(x => x.Article.CreationTime)
            .PageBy(input.SkipCount, input.MaxResultCount);

        var items = await AsyncExecuter.ToListAsync(pagedQuery);

        var dtos = items.Select(x => MapToDto(x.Article, x.CategoryName)).ToList();

        return new PagedResultDto<KnowledgeArticleDto>(totalCount, dtos);
    }

    public async Task<KnowledgeArticleDto> GetAsync(Guid id)
    {
        var article = await _articleRepository.GetAsync(id);
        var isManager = await AuthorizationService.IsGrantedAsync(HelpdeskPermissions.KnowledgeBase.Manage);

        if (!article.IsPublished && !isManager)
        {
            throw new BusinessException("Helpdesk:ArticleNotPublished", "Bài viết này chưa được xuất bản.");
        }

        article.IncrementViewCount();
        await _articleRepository.UpdateAsync(article, autoSave: true);

        var category = await _categoryRepository.FindAsync(article.CategoryId);
        return MapToDto(article, category?.Name);
    }

    public async Task<KnowledgeArticleDto> GetBySlugAsync(string slug)
    {
        var articleQuery = await _articleRepository.GetQueryableAsync();
        var article = await AsyncExecuter.FirstOrDefaultAsync(articleQuery.Where(x => x.Slug == slug));

        if (article == null)
        {
            throw new BusinessException("Helpdesk:ArticleNotFound", "Không tìm thấy bài viết.");
        }

        var isManager = await AuthorizationService.IsGrantedAsync(HelpdeskPermissions.KnowledgeBase.Manage);
        if (!article.IsPublished && !isManager)
        {
            throw new BusinessException("Helpdesk:ArticleNotPublished", "Bài viết này chưa được xuất bản.");
        }

        article.IncrementViewCount();
        await _articleRepository.UpdateAsync(article, autoSave: true);

        var category = await _categoryRepository.FindAsync(article.CategoryId);
        return MapToDto(article, category?.Name);
    }

    [Authorize(HelpdeskPermissions.KnowledgeBase.Create)]
    public async Task<KnowledgeArticleDto> CreateAsync(CreateUpdateKnowledgeArticleDto input)
    {
        var slug = string.IsNullOrWhiteSpace(input.Slug)
            ? GenerateSlug(input.Title)
            : GenerateSlug(input.Slug);

        var articleQuery = await _articleRepository.GetQueryableAsync();
        var existingWithSlug = await AsyncExecuter.AnyAsync(articleQuery.Where(x => x.Slug == slug));
        if (existingWithSlug)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        var article = new KnowledgeArticle(
            GuidGenerator.Create(),
            input.Title,
            slug,
            input.CategoryId,
            input.Content,
            input.Summary,
            input.Tags,
            input.IsPublished
        );

        await _articleRepository.InsertAsync(article, autoSave: true);
        var category = await _categoryRepository.FindAsync(article.CategoryId);
        return MapToDto(article, category?.Name);
    }

    [Authorize(HelpdeskPermissions.KnowledgeBase.Edit)]
    public async Task<KnowledgeArticleDto> UpdateAsync(Guid id, CreateUpdateKnowledgeArticleDto input)
    {
        var article = await _articleRepository.GetAsync(id);

        var slug = string.IsNullOrWhiteSpace(input.Slug)
            ? GenerateSlug(input.Title)
            : GenerateSlug(input.Slug);

        var articleQuery = await _articleRepository.GetQueryableAsync();
        var existingWithSlug = await AsyncExecuter.AnyAsync(articleQuery.Where(x => x.Slug == slug && x.Id != id));
        if (existingWithSlug)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        article.SetTitle(input.Title, slug);
        article.SetCategory(input.CategoryId);
        article.SetContent(input.Content, input.Summary);
        article.SetTags(input.Tags);
        article.SetPublished(input.IsPublished);

        await _articleRepository.UpdateAsync(article, autoSave: true);
        var category = await _categoryRepository.FindAsync(article.CategoryId);
        return MapToDto(article, category?.Name);
    }

    [Authorize(HelpdeskPermissions.KnowledgeBase.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _articleRepository.DeleteAsync(id);
    }

    public async Task VoteAsync(Guid id, bool isHelpful)
    {
        var article = await _articleRepository.GetAsync(id);
        article.Vote(isHelpful);
        await _articleRepository.UpdateAsync(article, autoSave: true);
    }

    public async Task<List<KnowledgeArticleSuggestionDto>> GetSuggestionsAsync(string query, int maxResultCount = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            return new List<KnowledgeArticleSuggestionDto>();
        }

        var filter = query.Trim().ToLower();

        var q = (await _articleRepository.GetQueryableAsync())
            .Where(x => x.IsPublished);

        var filteredQuery = q
            .Where(x => x.Title.ToLower().Contains(filter) ||
                        (x.Summary != null && x.Summary.ToLower().Contains(filter)) ||
                        (x.Tags != null && x.Tags.ToLower().Contains(filter)))
            .OrderByDescending(x => x.HelpfulCount)
            .ThenByDescending(x => x.ViewCount)
            .Take(maxResultCount);

        var articles = await AsyncExecuter.ToListAsync(filteredQuery);

        return articles.Select(x => new KnowledgeArticleSuggestionDto
        {
            Id = x.Id,
            Title = x.Title,
            Slug = x.Slug,
            Summary = x.Summary,
            HelpfulCount = x.HelpfulCount
        }).ToList();
    }

    public async Task<List<KnowledgeArticleDto>> GetPopularArticlesAsync(int count = 6)
    {
        var articleQuery = await _articleRepository.GetQueryableAsync();
        var categoryQuery = await _categoryRepository.GetQueryableAsync();

        var articlesQuery = (from a in articleQuery
                             where a.IsPublished
                             join c in categoryQuery on a.CategoryId equals c.Id into cats
                             from cat in cats.DefaultIfEmpty()
                             orderby a.ViewCount descending, a.HelpfulCount descending
                             select new { Article = a, CategoryName = cat != null ? cat.Name : null })
                             .Take(count);

        var articles = await AsyncExecuter.ToListAsync(articlesQuery);

        return articles.Select(x => MapToDto(x.Article, x.CategoryName)).ToList();
    }

    private static KnowledgeArticleDto MapToDto(KnowledgeArticle article, string? categoryName)
    {
        return new KnowledgeArticleDto
        {
            Id = article.Id,
            Title = article.Title,
            Slug = article.Slug,
            CategoryId = article.CategoryId,
            CategoryName = categoryName,
            Summary = article.Summary,
            Content = article.Content,
            Tags = article.Tags,
            IsPublished = article.IsPublished,
            ViewCount = article.ViewCount,
            HelpfulCount = article.HelpfulCount,
            NotHelpfulCount = article.NotHelpfulCount,
            CreationTime = article.CreationTime,
            CreatorId = article.CreatorId,
            LastModificationTime = article.LastModificationTime,
            LastModifierId = article.LastModifierId
        };
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var clean = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        clean = Regex.Replace(clean, @"[^a-z0-9\s-]", "");
        clean = Regex.Replace(clean, @"\s+", " ").Trim();
        clean = Regex.Replace(clean, @"\s", "-");

        return string.IsNullOrWhiteSpace(clean) ? Guid.NewGuid().ToString("N")[..8] : clean;
    }
}
