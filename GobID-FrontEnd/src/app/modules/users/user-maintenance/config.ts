import { Breadcrumb, StepForm } from '../../../models';

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Usuarios',
    path: '/users',
    active: false,
  },
  {
    label: 'Mantenimiento',
    path: '/users/maintenance',
    active: true,
  },
];

export const steps: StepForm = {
  current: 1,
  steps: [
    { id: 1, title: 'Informaciones generales' },
    { id: 2, title: 'Selección de Organizaciones' },
    { id: 3, title: 'Acceso a Aplicaciones' },
    { id: 4, title: 'Políticas' },
    { id: 5, title: 'Revisión y Confirmación' },
  ],
};
