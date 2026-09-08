import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from '@abp/ng.core';

export const SLA_ROUTES: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: 'policies',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.Sla.Policies' },
        loadComponent: () =>
          import('./sla-policies/sla-policies.component').then(c => c.SlaPoliciesComponent),
      },
      {
        path: 'business-hours',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.Sla.BusinessHours' },
        loadComponent: () =>
          import('./business-hours/business-hours.component').then(c => c.BusinessHoursComponent),
      },
      {
        path: 'compliance',
        canActivate: [permissionGuard],
        data: { requiredPolicy: 'Helpdesk.Sla.Reports' },
        loadComponent: () =>
          import('./sla-compliance/sla-compliance.component').then(c => c.SlaComplianceComponent),
      },
    ],
  },
];
