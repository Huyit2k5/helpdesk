import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PagedResultDto } from '@abp/ng.core';
import type {
  CreateUpdateTicketSourceDto,
  TicketSourceDto,
  TicketSourceGetListInput,
} from './models';

@Injectable({ providedIn: 'root' })
export class TicketSourceService {
  apiName = 'Default';
  constructor(private restService: RestService) {}

  create = (input: CreateUpdateTicketSourceDto) =>
    this.restService.request<any, TicketSourceDto>(
      { method: 'POST', url: '/api/app/ticket-source', body: input },
      { apiName: this.apiName }
    );

  update = (id: string, input: CreateUpdateTicketSourceDto) =>
    this.restService.request<any, TicketSourceDto>(
      { method: 'PUT', url: `/api/app/ticket-source/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: string) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/ticket-source/${id}` },
      { apiName: this.apiName }
    );

  get = (id: string) =>
    this.restService.request<any, TicketSourceDto>(
      { method: 'GET', url: `/api/app/ticket-source/${id}` },
      { apiName: this.apiName }
    );

  getList = (input: TicketSourceGetListInput) =>
    this.restService.request<any, PagedResultDto<TicketSourceDto>>(
      { method: 'GET', url: '/api/app/ticket-source', params: { ...input } },
      { apiName: this.apiName }
    );
}
