import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { UserApp } from './user-app';
import { UserAppMaintenance } from './user-app-maintenance/user-app-maintenance';

export const UserApplicationRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    children: [
      {
        path: 'user-application',
        component: UserApp,
      },
      {
        path: 'user-application/:userId',
        component: UserAppMaintenance,
      },
    ],
  },
];
