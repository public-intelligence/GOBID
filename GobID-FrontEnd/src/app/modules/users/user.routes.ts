import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { UserMaintenance } from './user-maintenance/user-maintenance';
import { User } from './user';
import { UserAppMaintenance } from './user-app-maintenance/user-app-maintenance';
import { UserPolicyMaintenance } from './user-policy-maintenance/user-policy-maintenance';

export const UserRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    children: [
      {
        path: 'users',
        component: User,
      },
      {
        path: 'users/create',
        component: UserMaintenance,
      },
      {
        path: 'users/edit/:id',
        component: UserMaintenance,
      },
      {
        path: 'users/link/:id',
        component: UserAppMaintenance,
      },
      {
        path: 'users/user-policy/:id',
        component: UserPolicyMaintenance,
      },
    ],
  },
];
