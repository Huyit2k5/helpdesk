import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type {
  AssetAuditSessionDto,
  CreateAssetAuditSessionDto,
  GetAssetAuditListInput,
  ScanAuditItemInput,
  ScanResultDto,
  ReconcileAuditItemsInput,
  AuditReportDto,
} from './models';

@Injectable({
  providedIn: 'root',
})
export class AssetAuditService {
  private restService = inject(RestService);
  apiName = 'Default';

  getList = (input: GetAssetAuditListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<AssetAuditSessionDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset-audit',
      params: {
        filter: input.filter,
        status: input.status,
        department: input.department,
        location: input.location,
        sorting: input.sorting,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount,
      },
    },
    { apiName: this.apiName, ...config });

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetAuditSessionDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-audit/${id}`,
    },
    { apiName: this.apiName, ...config });

  create = (input: CreateAssetAuditSessionDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetAuditSessionDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset-audit',
      body: input,
    },
    { apiName: this.apiName, ...config });

  start = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetAuditSessionDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-audit/${id}/start`,
    },
    { apiName: this.apiName, ...config });

  scanItem = (auditSessionId: string, input: ScanAuditItemInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ScanResultDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-audit/${auditSessionId}/scan-item`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  reconcile = (auditSessionId: string, input: ReconcileAuditItemsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetAuditSessionDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-audit/${auditSessionId}/reconcile`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  complete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetAuditSessionDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-audit/${id}/complete`,
    },
    { apiName: this.apiName, ...config });

  cancel = (id: string, reason?: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetAuditSessionDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-audit/${id}/cancel`,
      params: { reason },
    },
    { apiName: this.apiName, ...config });

  getReport = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AuditReportDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-audit/${id}/report`,
    },
    { apiName: this.apiName, ...config });
}
