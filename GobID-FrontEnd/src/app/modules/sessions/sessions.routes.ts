import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { Sessions } from './sessions';

export const SessionRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    children: [
      {
        path: 'sessions',
        component: Sessions,
      },
    ],
  },
];
