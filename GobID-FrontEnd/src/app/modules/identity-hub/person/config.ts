export * from './person-maintenance/config';
import { Breadcrumb, Column, Filter } from '../../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Identidad', path: '', active: false },
  { label: 'Personal', path: '/identity-hub/person', active: true },
];

export const headers = (): Column[] => [
  { value: 'Nombre', dataType: 'string' },
  { value: 'Apellido', dataType: 'string' },
  { value: 'Documento de Identidad', dataType: 'string' },
  { value: 'Email', dataType: 'string' },
  { value: 'Teléfono', dataType: 'string' },
  { value: 'Estado', dataType: 'string' },
];

export const filters: Filter[] = [
  {
    title: 'Estado',
    values: [
      {
        label: 'Activas',
        value: '1',
      },
      {
        label: 'Inactivas',
        value: '2',
      },
      {
        label: 'Suspendidas',
        value: '0',
      },
    ],
    type: 'select',
    controlName: 'status',
    defaultValue: '1',
  },
];
