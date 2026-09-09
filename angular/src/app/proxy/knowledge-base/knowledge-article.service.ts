import type { CreateUpdateKnowledgeArticleDto, GetKnowledgeArticleListInput, KnowledgeArticleDto, KnowledgeArticleSuggestionDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class KnowledgeArticleService {
  private restService = inject(RestService);
  apiName = 'Default';

  getList = (input: GetKnowledgeArticleListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<KnowledgeArticleDto>>({
      method: 'GET',
      url: '/api/app/knowledge-article',
      params: { filter: input.filter, categoryId: input.categoryId, isPublished: input.isPublished, tag: input.tag, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName, ...config });

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, KnowledgeArticleDto>({
      method: 'GET',
      url: `/api/app/knowledge-article/${id}`,
    },
    { apiName: this.apiName, ...config });

  getBySlug = (slug: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, KnowledgeArticleDto>({
      method: 'GET',
      url: `/api/app/knowledge-article/by-slug/${slug}`,
    },
    { apiName: this.apiName, ...config });

  create = (input: CreateUpdateKnowledgeArticleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, KnowledgeArticleDto>({
      method: 'POST',
      url: '/api/app/knowledge-article',
      body: input,
    },
    { apiName: this.apiName, ...config });

  update = (id: string, input: CreateUpdateKnowledgeArticleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, KnowledgeArticleDto>({
      method: 'PUT',
      url: `/api/app/knowledge-article/${id}`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/knowledge-article/${id}`,
    },
    { apiName: this.apiName, ...config });

  vote = (id: string, isHelpful: boolean, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/app/knowledge-article/${id}/vote`,
      params: { isHelpful },
    },
    { apiName: this.apiName, ...config });

  getSuggestions = (query: string, maxResultCount = 5, config?: Partial<Rest.Config>) =>
    this.restService.request<any, KnowledgeArticleSuggestionDto[]>({
      method: 'GET',
      url: '/api/app/knowledge-article/suggestions',
      params: { query, maxResultCount },
    },
    { apiName: this.apiName, ...config });

  getPopularArticles = (count = 6, config?: Partial<Rest.Config>) =>
    this.restService.request<any, KnowledgeArticleDto[]>({
      method: 'GET',
      url: '/api/app/knowledge-article/popular-articles',
      params: { count },
    },
    { apiName: this.apiName, ...config });
}
