import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PagedResultDto } from '@abp/ng.core';
import type {
  CannedResponseDto,
  CannedResponseGetListInput,
  CreateUpdateCannedResponseDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class CannedResponseService {
  apiName = 'Default';
  constructor(private restService: RestService) {}

  create = (input: CreateUpdateCannedResponseDto) =>
    this.restService.request<any, CannedResponseDto>(
      { method: 'POST', url: '/api/app/canned-response', body: input },
      { apiName: this.apiName }
    );

  update = (id: string, input: CreateUpdateCannedResponseDto) =>
    this.restService.request<any, CannedResponseDto>(
      { method: 'PUT', url: `/api/app/canned-response/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: string) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/canned-response/${id}` },
      { apiName: this.apiName }
    );

  get = (id: string) =>
    this.restService.request<any, CannedResponseDto>(
      { method: 'GET', url: `/api/app/canned-response/${id}` },
      { apiName: this.apiName }
    );

  getList = (input: CannedResponseGetListInput) =>
    this.restService.request<any, PagedResultDto<CannedResponseDto>>(
      { method: 'GET', url: '/api/app/canned-response', params: { ...input } },
      { apiName: this.apiName }
    );
}
