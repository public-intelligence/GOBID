import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { OrganizationApplication } from './organization-application';
import { LinkupOrgApps } from './linkup-org-app/linkup-org-app';
import { LinkupOrgAppsMaintenance } from './linkup-org-app-maintenance/linkup-org-app-maintenance';
import { permissionsGuard } from '../../guards';

export const OrganizationApplicationRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    data: {
      permissions: ['view_org_app'],
    },
    canActivate: [permissionsGuard],
    children: [
      {
        path: 'organization-application',
        component: OrganizationApplication,
      },
      {
        path: 'organization-application/organization/:id',
        component: LinkupOrgApps,
      },
      {
        path: 'organization-application/organization/:id/linkup-app',
        component: LinkupOrgAppsMaintenance,
      },
      {
        path: 'organization-application/organization/:id/linkup-app/edit/:idApp',
        component: LinkupOrgAppsMaintenance,
      },
    ],
  },
];
