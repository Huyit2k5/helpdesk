import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const DASHBOARD_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureDashboardRoutes();
  }),
];

function configureDashboardRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/dashboard',
      name: '::Menu:Dashboard',
      iconClass: 'fas fa-chart-line',
      order: 0,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Dashboard',
    },
  ]);
}
