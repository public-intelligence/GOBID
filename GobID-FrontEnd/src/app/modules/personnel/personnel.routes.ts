import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { permissionsGuard } from '../../guards';

export const PersonnelRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    data: {
      permissions: ['view_personnel'],
    },
    canActivate: [permissionsGuard],
    children: [
      {
        path: 'personnel',
        loadComponent: () =>
          import('./personnel').then((m) => m.Personnel),
      },
      {
        path: 'personnel/create',
        loadComponent: () =>
          import('./personnel-maintenance/personnel-maintenance').then(
            (m) => m.PersonnelMaintenance,
          ),
      },
      {
        path: 'personnel/edit/:id',
        loadComponent: () =>
          import('./personnel-maintenance/personnel-maintenance').then(
            (m) => m.PersonnelMaintenance,
          ),
      },
    ],
  },
];
