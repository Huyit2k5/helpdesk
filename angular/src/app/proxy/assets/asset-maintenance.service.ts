import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type {
  AssetMaintenanceDto,
  CreateAssetMaintenanceDto,
  CompleteAssetMaintenanceDto,
  AssetTcoSummaryDto,
  MaintenanceScheduleAlertDto,
  MaintenanceType,
  MaintenanceStatus,
} from './models';

export interface GetAssetMaintenanceListInput {
  assetId?: string | null;
  maintenanceType?: MaintenanceType | null;
  status?: MaintenanceStatus | null;
  filter?: string | null;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

@Injectable({
  providedIn: 'root',
})
export class AssetMaintenanceService {
  private restService = inject(RestService);
  apiName = 'Default';

  getList = (input: GetAssetMaintenanceListInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<AssetMaintenanceDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset-maintenance',
      params: {
        assetId: input.assetId,
        maintenanceType: input.maintenanceType,
        status: input.status,
        filter: input.filter,
        sorting: input.sorting,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount,
      },
    },
    { apiName: this.apiName, ...config });

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetMaintenanceDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-maintenance/${id}`,
    },
    { apiName: this.apiName, ...config });

  getByAssetId = (assetId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetMaintenanceDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-maintenance/by-asset-id/${assetId}`,
    },
    { apiName: this.apiName, ...config });

  create = (input: CreateAssetMaintenanceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetMaintenanceDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset-maintenance',
      body: input,
    },
    { apiName: this.apiName, ...config });

  complete = (id: string, input: CompleteAssetMaintenanceDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetMaintenanceDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-maintenance/${id}/complete`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  cancel = (id: string, reason: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-maintenance/${id}/cancel`,
      params: { reason },
    },
    { apiName: this.apiName, ...config });

  getTcoSummary = (assetId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetTcoSummaryDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset-maintenance/tco-summary/${assetId}`,
    },
    { apiName: this.apiName, ...config });

  getScheduleAlerts = (upcomingDays: number = 30, config?: Partial<Rest.Config>) =>
    this.restService.request<any, MaintenanceScheduleAlertDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset-maintenance/schedule-alerts',
      params: { upcomingDays },
    },
    { apiName: this.apiName, ...config });
}
