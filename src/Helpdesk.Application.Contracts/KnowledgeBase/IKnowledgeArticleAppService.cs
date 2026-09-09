using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helpdesk.KnowledgeBase.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Helpdesk.KnowledgeBase;

public interface IKnowledgeArticleAppService : IApplicationService
{
    Task<PagedResultDto<KnowledgeArticleDto>> GetListAsync(GetKnowledgeArticleListInput input);

    Task<KnowledgeArticleDto> GetAsync(Guid id);

    Task<KnowledgeArticleDto> GetBySlugAsync(string slug);

    Task<KnowledgeArticleDto> CreateAsync(CreateUpdateKnowledgeArticleDto input);

    Task<KnowledgeArticleDto> UpdateAsync(Guid id, CreateUpdateKnowledgeArticleDto input);

    Task DeleteAsync(Guid id);

    Task VoteAsync(Guid id, bool isHelpful);

    Task<List<KnowledgeArticleSuggestionDto>> GetSuggestionsAsync(string query, int maxResultCount = 5);

    Task<List<KnowledgeArticleDto>> GetPopularArticlesAsync(int count = 6);
}
