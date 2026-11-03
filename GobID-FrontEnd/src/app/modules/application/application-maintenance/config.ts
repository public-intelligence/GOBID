import { Breadcrumb, Column, StepForm } from '../../../models';

export const steps: StepForm = {
  current: 1,
  steps: [
    { id: 1, title: 'Informaciones generales' },
    { id: 2, title: 'Configuración de Acceso' },
    { id: 3, title: 'Revisión y Confirmación' },
  ],
};

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Aplicaciones',
    path: '/applications',
    active: false,
  },
  {
    label: 'Mantenimiento',
    path: '/applications/maintenance',
    active: true,
  },
];

export const permissionHeaders: Column[] = [
  { value: 'Código', justify: 'left', dataType: 'string' },
  { value: 'Nombre', justify: 'left', dataType: 'string' },
  { value: 'Descripción', justify: 'left', dataType: 'string' },
  { value: 'Automático', justify: 'center', dataType: 'boolean' },
];
