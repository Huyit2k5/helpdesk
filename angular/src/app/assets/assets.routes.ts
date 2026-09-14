import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';

export const ASSETS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./asset-list/asset-list.component').then(m => m.AssetListComponent),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'Helpdesk.Assets',
    },
  },
  {
    path: 'audits',
    loadComponent: () =>
      import('./asset-audit/asset-audit-list/asset-audit-list.component').then(m => m.AssetAuditListComponent),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'Helpdesk.Assets',
    },
  },
  {
    path: 'audits/:id',
    loadComponent: () =>
      import('./asset-audit/asset-audit-detail/asset-audit-detail.component').then(m => m.AssetAuditDetailComponent),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'Helpdesk.Assets',
    },
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./asset-detail/asset-detail.component').then(m => m.AssetDetailComponent),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'Helpdesk.Assets',
    },
  },
];
