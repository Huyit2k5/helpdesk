import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const ASSETS_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureAssetsRoutes();
  }),
];

function configureAssetsRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/assets',
      name: 'Tài Sản & Thiết Bị (ITAM)',
      iconClass: 'fas fa-laptop',
      order: 4,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Assets',
    },
  ]);
}
