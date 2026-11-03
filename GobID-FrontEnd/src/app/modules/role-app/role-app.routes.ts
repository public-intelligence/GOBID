import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { RoleApp } from './role-app';
import { Role } from './role/role';
import { permissionsGuard } from '../../guards';

export const RoleApplicationRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    data: {
      permissions: ['view_user'],
    },
    canActivate: [permissionsGuard],
    children: [
      {
        path: 'role-application',
        component: RoleApp,
      },
      {
        path: 'role-application/:orgAppId/roles',
        component: Role,
      },
    ],
  },
];
