import { Routes } from '@angular/router';
import { Application } from './application';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { ApplicationMaintenance } from './application-maintenance/application-maintenance';
import { AppCredentials } from './app-credentials/app-credentials';
import { permissionsGuard } from '../../guards';

export const ApplicationRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    data: {
      permissions: ['view_app'],
    },
    canActivate: [permissionsGuard],
    children: [
      {
        path: 'applications',
        component: Application,
      },
      {
        path: 'applications/create',
        component: ApplicationMaintenance,
      },
      {
        path: 'applications/edit/:id',
        component: ApplicationMaintenance,
      },
      {
        path: 'applications/:id/credentials',
        component: AppCredentials,
      },
    ],
  },
];
