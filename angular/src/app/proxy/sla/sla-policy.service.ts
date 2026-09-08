import type { CreateUpdateSlaPolicyDto, GetSlaPolicyListInput, SlaPolicyDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class SlaPolicyService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateSlaPolicyDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SlaPolicyDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/sla-policy',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/sla-policy/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SlaPolicyDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/sla-policy/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getActivePolicyList = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, SlaPolicyDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/sla-policy/active-policy-list',
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetSlaPolicyListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<SlaPolicyDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/sla-policy',
      params: { filter: input.filter, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateSlaPolicyDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SlaPolicyDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/sla-policy/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}