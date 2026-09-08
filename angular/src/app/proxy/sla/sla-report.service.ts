import type { GetSlaBreachListInput, SlaBreachLogDto, SlaComplianceStatsDto } from './dtos/models';
import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class SlaReportService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getBreachLogs = (input: GetSlaBreachListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<SlaBreachLogDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/sla-report/breach-logs',
      params: { breachType: input.breachType, startDate: input.startDate, endDate: input.endDate, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getComplianceStats = (startDate?: string, endDate?: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, SlaComplianceStatsDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/sla-report/compliance-stats',
      params: { startDate, endDate },
    },
    { apiName: this.apiName,...config });
}