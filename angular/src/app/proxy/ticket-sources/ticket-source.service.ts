import type { CreateUpdateTicketSourceDto, TicketSourceDto, TicketSourceGetListInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TicketSourceService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateTicketSourceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketSourceDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/ticket-source',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/ticket-source/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketSourceDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket-source/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TicketSourceGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TicketSourceDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/ticket-source',
      params: { filter: input.filter, isActive: input.isActive, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTicketSourceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketSourceDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket-source/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}