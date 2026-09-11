import type { CreateUpdateMacroDto, GetMacroListInput, MacroDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class MacroService {
  private restService = inject(RestService);
  apiName = 'Default';

  create = (input: CreateUpdateMacroDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MacroDto>(
      {
        method: 'POST',
        headers: { Accept: 'application/json' },
        url: '/api/app/macro',
        body: input,
      },
      { apiName: this.apiName, ...config }
    );

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>(
      {
        method: 'DELETE',
        url: `/api/app/macro/${id}`,
      },
      { apiName: this.apiName, ...config }
    );

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MacroDto>(
      {
        method: 'GET',
        headers: { Accept: 'application/json' },
        url: `/api/app/macro/${id}`,
      },
      { apiName: this.apiName, ...config }
    );

  getList = (input: GetMacroListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<MacroDto>>(
      {
        method: 'GET',
        headers: { Accept: 'application/json' },
        url: '/api/app/macro',
        params: {
          filter: input.filter,
          isActive: input.isActive,
          sorting: input.sorting,
          skipCount: input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName, ...config }
    );

  getActiveMacros = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, MacroDto[]>(
      {
        method: 'GET',
        headers: { Accept: 'application/json' },
        url: '/api/app/macro/active-macros',
      },
      { apiName: this.apiName, ...config }
    );

  update = (id: string, input: CreateUpdateMacroDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MacroDto>(
      {
        method: 'PUT',
        headers: { Accept: 'application/json' },
        url: `/api/app/macro/${id}`,
        body: input,
      },
      { apiName: this.apiName, ...config }
    );

  toggleActive = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MacroDto>(
      {
        method: 'POST',
        url: `/api/app/macro/${id}/toggle-active`,
      },
      { apiName: this.apiName, ...config }
    );
}
