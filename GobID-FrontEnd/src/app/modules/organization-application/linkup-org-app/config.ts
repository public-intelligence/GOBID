import { Breadcrumb, Column } from '../../../models';

export const breadcrumbs: Breadcrumb[] = [
  {
    label: 'Vinculación Organizacion-Aplicaciones',
    path: 'organization-application',
    active: false,
  },
  {
    label: 'Aplicaciones Vinculadas',
    path: 'organization-application/organization',
    active: true,
  },
];

export const headers: Column[] = [
  {
    value: 'Codigo',
    dataType: 'string',
  },
  {
    value: 'Nombre',
    dataType: 'string',
  },
  {
    value: 'Enlace',
    dataType: 'string',
  },
  {
    value: 'Estado',
    dataType: 'string',
  },
];
