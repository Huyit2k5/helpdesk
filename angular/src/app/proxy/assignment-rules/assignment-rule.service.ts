import type { AgentLookupDto, AssignmentRuleDto, AssignmentRuleGetListInput, CreateUpdateAssignmentRuleDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AssignmentRuleService {
  private restService = inject(RestService);
  apiName = 'Default';

  create = (input: CreateUpdateAssignmentRuleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssignmentRuleDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/assignment-rule',
      body: input,
    },
    { apiName: this.apiName, ...config });

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/assignment-rule/${id}`,
    },
    { apiName: this.apiName, ...config });

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssignmentRuleDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/assignment-rule/${id}`,
    },
    { apiName: this.apiName, ...config });

  getList = (input: AssignmentRuleGetListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<AssignmentRuleDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/assignment-rule',
      params: { filter: input.filter, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName, ...config });

  update = (id: string, input: CreateUpdateAssignmentRuleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssignmentRuleDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/assignment-rule/${id}`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  toggleActive = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssignmentRuleDto>({
      method: 'POST',
      url: `/api/app/assignment-rule/${id}/toggle-active`,
    },
    { apiName: this.apiName, ...config });

  getAgentLookup = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, AgentLookupDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/assignment-rule/agent-lookup',
    },
    { apiName: this.apiName, ...config });
}
