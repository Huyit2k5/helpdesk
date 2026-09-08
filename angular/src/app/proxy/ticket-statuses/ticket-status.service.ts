import type { CreateUpdateTicketStatusDto, TicketStatusDto, TicketStatusGetListInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TicketStatusService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateTicketStatusDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketStatusDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/ticket-status',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/ticket-status/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketStatusDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket-status/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: TicketStatusGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TicketStatusDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/ticket-status',
      params: { filter: input.filter, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTicketStatusDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TicketStatusDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/ticket-status/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}