import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from '@abp/ng.core';

export const MASTER_DATA_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'categories',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.Categories' },
        loadComponent: () =>
          import('./categories/categories.component').then(c => c.CategoriesComponent),
      },
      {
        path: 'priorities',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.Priorities' },
        loadComponent: () =>
          import('./priorities/priorities.component').then(c => c.PrioritiesComponent),
      },
      {
        path: 'departments',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.Departments' },
        loadComponent: () =>
          import('./departments/departments.component').then(c => c.DepartmentsComponent),
      },
      {
        path: 'ticket-statuses',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.TicketStatuses' },
        loadComponent: () =>
          import('./ticket-statuses/ticket-statuses.component').then(
            c => c.TicketStatusesComponent
          ),
      },
      {
        path: 'ticket-sources',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.TicketSources' },
        loadComponent: () =>
          import('./ticket-sources/ticket-sources.component').then(
            c => c.TicketSourcesComponent
          ),
      },
      {
        path: 'canned-responses',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.CannedResponses' },
        loadComponent: () =>
          import('./canned-responses/canned-responses.component').then(
            c => c.CannedResponsesComponent
          ),
      },
      {
        path: 'assignment-rules',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.AssignmentRules' },
        loadComponent: () =>
          import('./assignment-rules/assignment-rules.component').then(
            c => c.AssignmentRulesComponent
          ),
      },
    ],
  },
];
