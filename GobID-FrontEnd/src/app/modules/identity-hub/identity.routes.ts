import { Routes } from '@angular/router';
import { PanelLayoutComponent } from '../../components/ui/layouts/panel/panel.layout';
import { permissionsGuard } from '../../guards';
import { PersonMaintenance } from './person/person-maintenance/person-maintenance';
import { Person } from './person/person';
import { LegalEntity } from './legal-entity/legal-entity';
import { LegalEntityMaintenance } from './legal-entity/legal-entity-maintenance/legal-entity-maintenance';

export const IdentityHubRoutes: Routes = [
  {
    path: '',
    component: PanelLayoutComponent,
    data: {
      permissions: ['view_identity_hub'],
    },
    canActivate: [permissionsGuard],
    children: [
      {
        path: 'identity-hub/person',
        component: Person,
      },
      {
        path: 'identity-hub/person/create',
        component: PersonMaintenance,
      },
      {
        path: 'identity-hub/person/edit/:id',
        component: PersonMaintenance,
      },
      {
        path: 'identity-hub/legal-entity',
        component: LegalEntity,
      },
      {
        path: 'identity-hub/legal-entity/create',
        component: LegalEntityMaintenance,
      },
      {
        path: 'identity-hub/legal-entity/edit/:id',
        component: LegalEntityMaintenance,
      },
    ],
  },
];
