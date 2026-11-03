import { Breadcrumb } from '../../../models';

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Relación Usuario-Aplicación',
    path: '/user-application',
    active: false,
  },
  {
    label: 'Vincular',
    path: '/user-application/create',
    active: true,
  },
];
