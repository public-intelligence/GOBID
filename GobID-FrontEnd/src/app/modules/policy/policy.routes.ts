import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { permissionsGuard } from '../../guards';
import { PolicyComponent } from './policy';
import { PolicyMaintenance } from './policy-maintenance/policy-maintenance';

export const PolicyRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    data: {
      permissions: ['view_policy'],
    },
    canActivate: [permissionsGuard],
    children: [
      {
        path: 'policy',
        component: PolicyComponent,
      },
      {
        path: 'policy/create',
        component: PolicyMaintenance,
      },
      {
        path: 'policy/edit/:id',
        component: PolicyMaintenance,
      },
    ],
  },
];
