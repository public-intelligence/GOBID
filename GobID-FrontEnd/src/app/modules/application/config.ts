import { Breadcrumb, Column, Filter } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Aplicaciones', path: '/application', active: true },
];

export const headers = (isLocalAdmin: boolean): Column[] => [
  {
    value: 'ID',
    dataType: 'string',
  },
  {
    value: 'Código',
    dataType: 'string',
  },
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'Descripción',
    dataType: 'string',
  },
  ...(isLocalAdmin
    ? [{ value: 'Enlace Login', dataType: 'string' as const }]
    : []),
  {
    value: 'Fecha de Creación',
    dataType: 'date',
  },
  {
    value: 'Pública',
    justify: 'center',
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
    type: 'select',
    controlName: 'IsActive',
    defaultValue: 'true',
  },
];
