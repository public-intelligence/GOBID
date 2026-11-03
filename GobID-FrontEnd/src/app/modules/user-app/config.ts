import { Breadcrumb, Column } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Relación Usuario-Aplicación',
    path: '/user-application',
    active: true,
  },
];

export const headers: Column[] = [
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'Apellido',
    dataType: 'string',
  },
  {
    value: 'Aplicaciones Vinculadas',
    dataType: 'string',
    justify: 'center',
  },
];
