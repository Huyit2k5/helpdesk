import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const SLA_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureSlaRoutes();
  }),
];

function configureSlaRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/sla',
      name: '::Menu:Sla',
      iconClass: 'fas fa-stopwatch',
      order: 4,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Sla || Helpdesk.Sla.Policies || Helpdesk.Sla.BusinessHours || Helpdesk.Sla.Reports',
    },
    {
      path: '/sla/policies',
      name: '::Menu:SlaPolicies',
      parentName: '::Menu:Sla',
      iconClass: 'fas fa-handshake',
      order: 1,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Sla.Policies',
    },
    {
      path: '/sla/business-hours',
      name: '::Menu:BusinessHours',
      parentName: '::Menu:Sla',
      iconClass: 'fas fa-calendar-alt',
      order: 2,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Sla.BusinessHours',
    },
    {
      path: '/sla/compliance',
      name: '::Menu:SlaCompliance',
      parentName: '::Menu:Sla',
      iconClass: 'fas fa-chart-pie',
      order: 3,
      layout: eLayoutType.application,
      requiredPolicy: 'Helpdesk.Sla.Reports',
    },
  ]);
}
