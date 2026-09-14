import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';
export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];
function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
      {
        path: '/tickets',
        name: '::Menu:Tickets',
        iconClass: 'fas fa-ticket-alt',
        order: 2,
        layout: eLayoutType.application,
      },
      {
        path: '/discord-link',
        name: 'Liên kết Discord',
        iconClass: 'fa-brands fa-discord',
        order: 9,
        layout: eLayoutType.application,
      },
  ]);
}
