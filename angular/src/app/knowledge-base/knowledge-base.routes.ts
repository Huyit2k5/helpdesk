import { Routes } from '@angular/router';
import { permissionGuard } from '@abp/ng.core';

export const KNOWLEDGE_BASE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./knowledge-base.component').then(c => c.KnowledgeBaseComponent),
  },
  {
    path: 'manage',
    loadComponent: () =>
      import('./article-manage.component').then(c => c.ArticleManageComponent),
    canActivate: [permissionGuard],
    data: {
      requiredPolicy: 'Helpdesk.KnowledgeBase.Create',
    },
  },
  {
    path: ':slug',
    loadComponent: () =>
      import('./article-detail.component').then(c => c.ArticleDetailComponent),
  },
];
