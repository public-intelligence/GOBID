import { Breadcrumb, Column } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Vinculación Organizacion-Aplicaciones', path: '', active: true },
];

export const headers: Column[] = [
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'Sigla',
    dataType: 'string',
  },
  {
    value: 'Aplicacion Vinculadas',
    dataType: 'number',
    justify: 'center',
  },
];
