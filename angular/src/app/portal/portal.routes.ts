import { Routes } from '@angular/router';
import { authGuard } from '@abp/ng.core';

export const PORTAL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./portal-home.component').then(c => c.PortalHomeComponent),
  },
  {
    path: 'my-tickets',
    loadComponent: () =>
      import('./my-tickets.component').then(c => c.MyTicketsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'tickets/:id',
    loadComponent: () =>
      import('./ticket-view.component').then(c => c.TicketViewComponent),
    canActivate: [authGuard],
  },
  {
    path: 'my-assets',
    loadComponent: () =>
      import('./my-assets/my-assets.component').then(c => c.MyAssetsComponent),
    canActivate: [authGuard],
  },
];
