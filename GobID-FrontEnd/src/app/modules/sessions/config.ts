import { Breadcrumb, Column } from '../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Sesiones', path: '/sessions', active: true },
];

export const headers: Column[] = [
  { value: 'Email', dataType: 'string' },
  { value: 'Nombre', dataType: 'string' },
  { value: 'IP', dataType: 'string' },
  { value: 'Aplicación', dataType: 'string' },
  { value: 'Última Actividad', dataType: 'string' },
  { value: 'Expira', dataType: 'string' },
];
