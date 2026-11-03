import { Breadcrumb, Column, Filter } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Organizaciones', path: '/organizations', active: true },
];

export const headers: Column[] = [
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'RNC/Cédula',
    dataType: 'string',
  },
  {
    value: 'Sigla',
    dataType: 'string',
  },
  {
    value: 'Dgii Estado',
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
