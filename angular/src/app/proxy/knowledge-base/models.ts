import type { FullAuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface KnowledgeArticleDto extends FullAuditedEntityDto<string> {
  title: string;
  slug: string;
  categoryId: string;
  categoryName?: string;
  summary?: string;
  content: string;
  tags?: string;
  isPublished: boolean;
  viewCount: number;
  helpfulCount: number;
  notHelpfulCount: number;
}

export interface CreateUpdateKnowledgeArticleDto {
  title: string;
  slug?: string;
  categoryId: string;
  summary?: string;
  content: string;
  tags?: string;
  isPublished: boolean;
}

export interface GetKnowledgeArticleListInput extends PagedAndSortedResultRequestDto {
  filter?: string;
  categoryId?: string;
  isPublished?: boolean;
  tag?: string;
}

export interface KnowledgeArticleSuggestionDto {
  id: string;
  title: string;
  slug: string;
  summary?: string;
  helpfulCount: number;
}
