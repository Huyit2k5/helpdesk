import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const PORTAL_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configurePortalRoutes();
  }),
];

function configurePortalRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/portal',
      name: '::Menu:CustomerPortal',
      iconClass: 'fas fa-life-ring',
      order: 1,
      layout: eLayoutType.application,
    },
  ]);
}
