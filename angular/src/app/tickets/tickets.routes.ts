import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from '@abp/ng.core';

export const TICKETS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./tickets.component').then(m => m.TicketsComponent),
    canActivate: [authGuard],
  },
  {
    path: ':id',
    loadComponent: () => import('./ticket-detail/ticket-detail.component').then(m => m.TicketDetailComponent),
    canActivate: [authGuard],
  },
];
