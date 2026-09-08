import type { CategoryDto, CategoryGetListInput, CategoryLookupDto, CreateUpdateCategoryDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateCategoryDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CategoryDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/category',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/category/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CategoryDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/category/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CategoryGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CategoryDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/category',
      params: { filter: input.filter, parentId: input.parentId, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getLookup = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<CategoryLookupDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/category/lookup',
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCategoryDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CategoryDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/category/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}