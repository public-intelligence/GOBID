import { Breadcrumb, Column, Filter } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Usuarios', path: '/users', active: true },
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
    value: 'Email',
    dataType: 'string',
  },
  {
    value: 'Organización',
    dataType: 'string',
  },
  {
    value: 'Estado',
    dataType: 'string',
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
        label: 'Sin Filtro',
        value: '',
      },
    ],
    controlName: 'IsActive',
    defaultValue: 'true',
    type: 'select',
  },
];
