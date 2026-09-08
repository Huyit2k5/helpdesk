import type { CreateUpdatePriorityDto, PriorityDto, PriorityGetListInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PriorityService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdatePriorityDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PriorityDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/priority',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/priority/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PriorityDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/priority/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PriorityGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<PriorityDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/priority',
      params: { filter: input.filter, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdatePriorityDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PriorityDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/priority/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}