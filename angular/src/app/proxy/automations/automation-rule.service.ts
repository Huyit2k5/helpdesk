import type { AutomationRuleDto, CreateUpdateAutomationRuleDto, GetAutomationRuleListInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AutomationRuleService {
  private restService = inject(RestService);
  apiName = 'Default';

  create = (input: CreateUpdateAutomationRuleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AutomationRuleDto>(
      {
        method: 'POST',
        headers: { Accept: 'application/json' },
        url: '/api/app/automation-rule',
        body: input,
      },
      { apiName: this.apiName, ...config }
    );

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>(
      {
        method: 'DELETE',
        url: `/api/app/automation-rule/${id}`,
      },
      { apiName: this.apiName, ...config }
    );

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AutomationRuleDto>(
      {
        method: 'GET',
        headers: { Accept: 'application/json' },
        url: `/api/app/automation-rule/${id}`,
      },
      { apiName: this.apiName, ...config }
    );

  getList = (input: GetAutomationRuleListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<AutomationRuleDto>>(
      {
        method: 'GET',
        headers: { Accept: 'application/json' },
        url: '/api/app/automation-rule',
        params: {
          filter: input.filter,
          triggerType: input.triggerType,
          isActive: input.isActive,
          sorting: input.sorting,
          skipCount: input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName, ...config }
    );

  update = (id: string, input: CreateUpdateAutomationRuleDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AutomationRuleDto>(
      {
        method: 'PUT',
        headers: { Accept: 'application/json' },
        url: `/api/app/automation-rule/${id}`,
        body: input,
      },
      { apiName: this.apiName, ...config }
    );

  toggleActive = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AutomationRuleDto>(
      {
        method: 'POST',
        url: `/api/app/automation-rule/${id}/toggle-active`,
      },
      { apiName: this.apiName, ...config }
    );
}
