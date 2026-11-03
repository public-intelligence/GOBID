import { Column, Filter } from '../../models';

export const headers: Column[] = [
  { value: 'Nombre', dataType: 'string' },
  { value: 'Posición', dataType: 'string' },
  { value: 'Localidad', dataType: 'string' },
  { value: 'Estado', dataType: 'string' },

];

export const filters: Filter[] = [
  {
    title: 'Estado',
    values: [
      { label: 'Activos', value: 'true' },
      { label: 'Inactivos', value: 'false' },
      { label: 'Sin Filtro', value: '' },
    ],
    controlName: 'isActive',
    defaultValue: 'true',
    type: 'select',
  },
];
