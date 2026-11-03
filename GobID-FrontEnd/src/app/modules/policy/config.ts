import { Breadcrumb, Column, Filter } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Políticas', path: '/policy', active: true },
];

export const headers: Column[] = [
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'Descripción',
    dataType: 'string',
  },
  {
    value: 'Efecto',
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
        label: 'Activos',
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
    type: 'select',
    controlName: 'IsActive',
    defaultValue: 'true',
  },
];
