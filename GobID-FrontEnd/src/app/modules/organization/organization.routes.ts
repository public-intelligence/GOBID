import { Routes } from '@angular/router';
import { OrganizationComponent } from './organization';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { OrganizationMaintenanceComponent } from './organization-maintenance/organization-maintenance';
import { permissionsGuard } from '../../guards';

export const OrganizationRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    data: {
      permissions: ['view_org'],
    },
    canActivate: [permissionsGuard],
    children: [
      {
        path: 'organizations',
        component: OrganizationComponent,
      },
      {
        path: 'organizations/create',
        component: OrganizationMaintenanceComponent,
      },
      {
        path: 'organizations/edit/:id',
        component: OrganizationMaintenanceComponent,
      },
    ],
  },
];
