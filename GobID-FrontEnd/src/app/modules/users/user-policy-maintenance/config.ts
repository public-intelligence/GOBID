import { Breadcrumb, StepForm } from '../../../models';

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Usuarios',
    path: '/users',
    active: false,
  },
  {
    label: 'Mantenimiento',
    path: 'users/user-policy-maintenance',
    active: true,
  },
];
