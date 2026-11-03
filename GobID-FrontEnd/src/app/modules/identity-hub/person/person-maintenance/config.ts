import { Breadcrumb } from '../../../../models';

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Identidad',
    path: '',
    active: false,
  },
  {
    label: 'Personal',
    path: '/identity-hub/person',
    active: false,
  },
  {
    label: 'Mantenimiento',
    path: '/identity-hub/person/maintenance',
    active: true,
  },
];
