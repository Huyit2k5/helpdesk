import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type { PagedResultDto } from '@abp/ng.core';
import type { CreateUpdateDepartmentDto, DepartmentDto, DepartmentGetListInput } from './models';

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  apiName = 'Default';
  constructor(private restService: RestService) {}

  create = (input: CreateUpdateDepartmentDto) =>
    this.restService.request<any, DepartmentDto>(
      { method: 'POST', url: '/api/app/department', body: input },
      { apiName: this.apiName }
    );

  update = (id: string, input: CreateUpdateDepartmentDto) =>
    this.restService.request<any, DepartmentDto>(
      { method: 'PUT', url: `/api/app/department/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: string) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/department/${id}` },
      { apiName: this.apiName }
    );

  get = (id: string) =>
    this.restService.request<any, DepartmentDto>(
      { method: 'GET', url: `/api/app/department/${id}` },
      { apiName: this.apiName }
    );

  getList = (input: DepartmentGetListInput) =>
    this.restService.request<any, PagedResultDto<DepartmentDto>>(
      { method: 'GET', url: '/api/app/department', params: { ...input } },
      { apiName: this.apiName }
    );
}
