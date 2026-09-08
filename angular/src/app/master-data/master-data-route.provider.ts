import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const MASTER_DATA_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureMasterDataRoutes();
  }),
];

function configureMasterDataRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/master-data',
      name: '::Menu:MasterData',
      iconClass: 'fas fa-database',
      order: 3,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Categories || Helpdesk.Priorities || Helpdesk.Departments',
    },
    {
      path: '/master-data/categories',
      name: '::Menu:Categories',
      parentName: '::Menu:MasterData',
      iconClass: 'fas fa-tags',
      order: 1,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Categories',
    },
    {
      path: '/master-data/priorities',
      name: '::Menu:Priorities',
      parentName: '::Menu:MasterData',
      iconClass: 'fas fa-flag',
      order: 2,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Priorities',
    },
    {
      path: '/master-data/departments',
      name: '::Menu:Departments',
      parentName: '::Menu:MasterData',
      iconClass: 'fas fa-building',
      order: 3,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Departments',
    },
    {
      path: '/master-data/ticket-statuses',
      name: '::Menu:TicketStatuses',
      parentName: '::Menu:MasterData',
      iconClass: 'fas fa-list-alt',
      order: 4,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.TicketStatuses',
    },
    {
      path: '/master-data/ticket-sources',
      name: '::Menu:TicketSources',
      parentName: '::Menu:MasterData',
      iconClass: 'fas fa-paper-plane',
      order: 5,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.TicketSources',
    },
    {
      path: '/master-data/canned-responses',
      name: '::Menu:CannedResponses',
      parentName: '::Menu:MasterData',
      iconClass: 'fas fa-comment-dots',
      order: 6,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.CannedResponses',
    },
  ]);
}
