import type { CannedResponseDto, CannedResponseGetListInput, CreateUpdateCannedResponseDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CannedResponseService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateCannedResponseDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CannedResponseDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/canned-response',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/canned-response/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CannedResponseDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/canned-response/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: CannedResponseGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<CannedResponseDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/canned-response',
      params: { filter: input.filter, categoryId: input.categoryId, isPublic: input.isPublic, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateCannedResponseDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CannedResponseDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/canned-response/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}