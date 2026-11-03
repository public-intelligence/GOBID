import { Breadcrumb, Column, Filter } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Aplicaciones', path: '/application', active: true },
];

export const headers: Column[] = [
  {
    value: 'Código',
    dataType: 'string',
  },
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'Roles Creados',
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
