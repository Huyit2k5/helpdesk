import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PagedResultDto } from '@abp/ng.core';
import type {
  CreateUpdateTicketStatusDto,
  TicketStatusDto,
  TicketStatusGetListInput,
} from './models';

@Injectable({ providedIn: 'root' })
export class TicketStatusService {
  apiName = 'Default';
  constructor(private restService: RestService) {}

  create = (input: CreateUpdateTicketStatusDto) =>
    this.restService.request<any, TicketStatusDto>(
      { method: 'POST', url: '/api/app/ticket-status', body: input },
      { apiName: this.apiName }
    );

  update = (id: string, input: CreateUpdateTicketStatusDto) =>
    this.restService.request<any, TicketStatusDto>(
      { method: 'PUT', url: `/api/app/ticket-status/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: string) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/ticket-status/${id}` },
      { apiName: this.apiName }
    );

  get = (id: string) =>
    this.restService.request<any, TicketStatusDto>(
      { method: 'GET', url: `/api/app/ticket-status/${id}` },
      { apiName: this.apiName }
    );

  getList = (input: TicketStatusGetListInput) =>
    this.restService.request<any, PagedResultDto<TicketStatusDto>>(
      { method: 'GET', url: '/api/app/ticket-status', params: { ...input } },
      { apiName: this.apiName }
    );
}
