import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';
export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(c => c.createRoutes()),
  },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },
  {
    path: 'tenant-management',
    loadChildren: () => import('@abp/ng.tenant-management').then(c => c.createRoutes()),
  },
  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },
  {
    path: 'tickets',
    loadChildren: () =>
      import('./tickets/tickets.routes').then(m => m.TICKETS_ROUTES),
  },
  {
    path: 'master-data',
    loadChildren: () =>
      import('./master-data/master-data.routes').then(m => m.MASTER_DATA_ROUTES),
  },
  {
    path: 'sla',
    loadChildren: () =>
      import('./sla/sla.routes').then(m => m.SLA_ROUTES),
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./dashboard/dashboard.component').then(c => c.DashboardComponent),
  },
];
