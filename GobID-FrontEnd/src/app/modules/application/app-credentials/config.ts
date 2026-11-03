import { Breadcrumb, Column } from '../../../models';

export const breadcrumbs = (applicationId: string): Breadcrumb[] => [
  { label: 'Aplicaciones', path: '/applications', active: false },
  { label: 'Credenciales M2M', path: `/applications/${applicationId}/credentials`, active: true },
];

export const headers: Column[] = [
  { value: 'Descripción', dataType: 'string' },
  { value: 'Estado', dataType: 'string', justify: 'center' },
  { value: 'Vence', dataType: 'string' },
  { value: 'Último Uso', dataType: 'string' },
  { value: 'Creada', dataType: 'date' },
];
