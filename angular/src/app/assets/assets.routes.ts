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
    path: ':id',
    loadComponent: () =>
      import('./asset-detail/asset-detail.component').then(m => m.AssetDetailComponent),
    canActivate: [authGuard, permissionGuard],
    data: {
      requiredPolicy: 'Helpdesk.Assets',
    },
  },
];
