import { RestService, Rest } from '@abp/ng.core';
import type { PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type {
  AssetDetailDto,
  AssetDto,
  AssetKpiDto,
  AssignAssetDto,
  ChangeAssetStatusDto,
  CreateAssetDto,
  GetAssetsInput,
  ReturnAssetDto,
  UpdateAssetDto
} from './models';

@Injectable({
  providedIn: 'root',
})
export class AssetService {
  private restService = inject(RestService);
  apiName = 'Default';

  getList = (input: GetAssetsInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<AssetDto>>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset',
      params: {
        filter: input.filter,
        assetType: input.assetType,
        status: input.status,
        department: input.department,
        assignedToUserId: input.assignedToUserId,
        sorting: input.sorting,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount,
      },
    },
    { apiName: this.apiName, ...config });

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetDetailDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset/${id}`,
    },
    { apiName: this.apiName, ...config });

  create = (input: CreateAssetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetDto>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset',
      body: input,
    },
    { apiName: this.apiName, ...config });

  update = (id: string, input: UpdateAssetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetDto>({
      method: 'PUT',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset/${id}`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/asset/${id}`,
    },
    { apiName: this.apiName, ...config });

  assign = (id: string, input: AssignAssetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset/${id}/assign`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  return = (id: string, input?: ReturnAssetDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset/${id}/return`,
      body: input || {},
    },
    { apiName: this.apiName, ...config });

  changeStatus = (id: string, input: ChangeAssetStatusDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset/${id}/change-status`,
      body: input,
    },
    { apiName: this.apiName, ...config });

  getKpis = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetKpiDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset/kpis',
    },
    { apiName: this.apiName, ...config });

  getLookup = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset/lookup',
    },
    { apiName: this.apiName, ...config });

  getMyAssets = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetDto[]>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset/my-assets',
    },
    { apiName: this.apiName, ...config });

  getReceipt = (id: string, type: string = 'handover', config?: Partial<Rest.Config>) =>
    this.restService.request<any, any>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: `/api/app/asset/${id}/receipt`,
      params: { type },
    },
    { apiName: this.apiName, ...config });

  getByAssetTag = (assetTag: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, AssetDto>({
      method: 'GET',
      headers: { Accept: 'application/json' },
      url: '/api/app/asset/by-asset-tag',
      params: { assetTag },
    },
    { apiName: this.apiName, ...config });
}
