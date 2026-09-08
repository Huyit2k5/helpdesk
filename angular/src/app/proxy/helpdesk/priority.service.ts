import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PagedResultDto } from '@abp/ng.core';
import type { CreateUpdatePriorityDto, PriorityDto, PriorityGetListInput } from './models';

@Injectable({ providedIn: 'root' })
export class PriorityService {
  apiName = 'Default';
  constructor(private restService: RestService) {}

  create = (input: CreateUpdatePriorityDto) =>
    this.restService.request<any, PriorityDto>(
      { method: 'POST', url: '/api/app/priority', body: input },
      { apiName: this.apiName }
    );

  update = (id: string, input: CreateUpdatePriorityDto) =>
    this.restService.request<any, PriorityDto>(
      { method: 'PUT', url: `/api/app/priority/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: string) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/priority/${id}` },
      { apiName: this.apiName }
    );

  get = (id: string) =>
    this.restService.request<any, PriorityDto>(
      { method: 'GET', url: `/api/app/priority/${id}` },
      { apiName: this.apiName }
    );

  getList = (input: PriorityGetListInput) =>
    this.restService.request<any, PagedResultDto<PriorityDto>>(
      { method: 'GET', url: '/api/app/priority', params: { ...input } },
      { apiName: this.apiName }
    );
}
