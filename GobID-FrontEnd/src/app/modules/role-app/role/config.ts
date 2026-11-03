import { Breadcrumb, Column, Filter } from '../../../models';

export const breadcrumbs = (id: string): Breadcrumb[] => [
  { label: 'Aplicaciones', path: '/role-application', active: false },
  { label: 'roles', path: `/role-application/${id}/roles`, active: true },
];

export const headers: Column[] = [
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'Usuarios vinculados',
    dataType: 'string',
    justify: 'center',
  },
  {
    value: 'Cantidad de Permisos',
    dataType: 'string',
    justify: 'center',
  },
];

export const filters: Filter[] = [
  {
    title: 'Estado',
    values: [
      {
        label: 'Activas',
        value: 'true',
      },
      {
        label: 'Inactivas',
        value: 'false',
      },
      {
        label: 'Ambas',
        value: '',
      },
    ],
    type: 'select',
    controlName: 'IsActive',
    defaultValue: 'true',
  },
];
