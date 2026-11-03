import { Breadcrumb, StepForm } from '../../../models';

export const steps: StepForm = {
  current: 1,
  steps: [
    { id: 1, title: 'Seleccionar Aplicación' },
    { id: 2, title: 'Seleccionar tipo de Acceso' },
    { id: 3, title: 'Revisión y Confirmación' },
  ],
};

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Vinculación Organizacion-Aplicaciones',
    path: 'organizations-application',
    active: false,
  },
  {
    label: 'Aplicaciones Vinculadas',
    path: 'organization-application/organization',
    active: false,
  },
  {
    label: 'Vincular Aplicación',
    path: 'organization-application/organization/linkup-app',
    active: true,
  },
];
