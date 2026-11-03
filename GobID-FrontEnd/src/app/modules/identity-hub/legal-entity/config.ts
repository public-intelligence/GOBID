import { Column, Filter } from '../../../models';

export const breadcrumbs = [
  { label: 'Identidad', path: '', active: false },
  { label: 'Organizaciones', path: '/identity-hub/organization', active: true },
];

export const headers = (): Column[] => [
  { value: 'Nombre Legal', dataType: 'string' },
  { value: 'Nombre Social', dataType: 'string' },
  { value: 'RNC', dataType: 'string' },
  { value: 'Correo Electrónico', dataType: 'string' },
  { value: 'Teléfono', dataType: 'string' },
  { value: 'Tipo', dataType: 'string' },
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
