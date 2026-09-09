import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';

export const KNOWLEDGE_BASE_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureKnowledgeBaseRoutes();
  }),
];

function configureKnowledgeBaseRoutes() {
  const routes = inject(RoutesService);
  routes.add([
    {
      path: '/knowledge-base',
      name: '::Menu:KnowledgeBase',
      iconClass: 'fas fa-book',
      order: 3,
      layout: eLayoutType.application,
    },
  ]);
}
