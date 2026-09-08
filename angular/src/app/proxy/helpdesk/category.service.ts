import { RestService, Rest } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PagedResultDto } from '@abp/ng.core';
import type {
  CategoryDto,
  CategoryGetListInput,
  CategoryLookupDto,
  CreateUpdateCategoryDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  create = (input: CreateUpdateCategoryDto) =>
    this.restService.request<any, CategoryDto>(
      { method: 'POST', url: '/api/app/category', body: input },
      { apiName: this.apiName }
    );

  update = (id: string, input: CreateUpdateCategoryDto) =>
    this.restService.request<any, CategoryDto>(
      { method: 'PUT', url: `/api/app/category/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: string) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/category/${id}` },
      { apiName: this.apiName }
    );

  get = (id: string) =>
    this.restService.request<any, CategoryDto>(
      { method: 'GET', url: `/api/app/category/${id}` },
      { apiName: this.apiName }
    );

  getList = (input: CategoryGetListInput) =>
    this.restService.request<any, PagedResultDto<CategoryDto>>(
      { method: 'GET', url: '/api/app/category', params: { ...input } },
      { apiName: this.apiName }
    );

  getLookup = () =>
    this.restService.request<any, CategoryLookupDto[]>(
      { method: 'GET', url: '/api/app/category/lookup' },
      { apiName: this.apiName }
    );
}
