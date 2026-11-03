import { Breadcrumb, Select } from '../../../models';

export const breadcrumbs: Breadcrumb[] = [
  { label: 'Políticas', path: '/policy', active: false },
  { label: 'Mantenimiento', path: 'policy/create', active: true },
];

export interface ConditionKeyConfig {
  label: string;
  value: string;
  inputType: 'text' | 'time' | 'timezone' | 'days';
}

export const EFFECT_OPTIONS: Select[] = [
  { label: 'Permitir Acceso', value: 'ALLOW' },
  { label: 'Denegar Acceso', value: 'DENY' },
];

export const TIMEZONE_OPTIONS: Select[] = [
  { label: 'UTC-12:00', value: 'UTC-12:00' },
  { label: 'UTC-11:00', value: 'UTC-11:00' },
  { label: 'UTC-10:00', value: 'UTC-10:00' },
  { label: 'UTC-09:00', value: 'UTC-09:00' },
  { label: 'UTC-08:00', value: 'UTC-08:00' },
  { label: 'UTC-07:00', value: 'UTC-07:00' },
  { label: 'UTC-06:00', value: 'UTC-06:00' },
  { label: 'UTC-05:00', value: 'UTC-05:00' },
  { label: 'UTC-04:30', value: 'UTC-04:30' },
  { label: 'UTC-04:00', value: 'UTC-04:00' },
  { label: 'UTC-03:00', value: 'UTC-03:00' },
  { label: 'UTC-02:00', value: 'UTC-02:00' },
  { label: 'UTC-01:00', value: 'UTC-01:00' },
  { label: 'UTC+00:00', value: 'UTC+00:00' },
  { label: 'UTC+01:00', value: 'UTC+01:00' },
  { label: 'UTC+02:00', value: 'UTC+02:00' },
  { label: 'UTC+03:00', value: 'UTC+03:00' },
  { label: 'UTC+03:30', value: 'UTC+03:30' },
  { label: 'UTC+04:00', value: 'UTC+04:00' },
  { label: 'UTC+04:30', value: 'UTC+04:30' },
  { label: 'UTC+05:00', value: 'UTC+05:00' },
  { label: 'UTC+05:30', value: 'UTC+05:30' },
  { label: 'UTC+05:45', value: 'UTC+05:45' },
  { label: 'UTC+06:00', value: 'UTC+06:00' },
  { label: 'UTC+06:30', value: 'UTC+06:30' },
  { label: 'UTC+07:00', value: 'UTC+07:00' },
  { label: 'UTC+08:00', value: 'UTC+08:00' },
  { label: 'UTC+09:00', value: 'UTC+09:00' },
  { label: 'UTC+09:30', value: 'UTC+09:30' },
  { label: 'UTC+10:00', value: 'UTC+10:00' },
  { label: 'UTC+11:00', value: 'UTC+11:00' },
  { label: 'UTC+12:00', value: 'UTC+12:00' },
  { label: 'UTC+13:00', value: 'UTC+13:00' },
  { label: 'UTC+14:00', value: 'UTC+14:00' },
];

export const APPLY_ON_DAYS: Select[] = [
  { label: 'Lunes', value: 'Monday' },
  { label: 'Martes', value: 'Tuesday' },
  { label: 'Miercoles', value: 'Wednesday' },
  { label: 'Jueves', value: 'Thursday' },
  { label: 'Viernes', value: 'Friday' },
  { label: 'Sabado', value: 'Saturday' },
  { label: 'Domingo', value: 'Sunday' },
];

export const CONDITION_KEYS_BY_TYPE: Record<string, ConditionKeyConfig[]> = {
  IP_RANGE: [
    { label: 'IP Inicio', value: 'IpRangeStart', inputType: 'text' },
    { label: 'IP Fin', value: 'IpRangeEnd', inputType: 'text' },
  ],
  TIME_WINDOW: [
    { label: 'Hora inicio', value: 'StartTime', inputType: 'time' },
    { label: 'Hora fin', value: 'EndTime', inputType: 'time' },
    { label: 'Zona horaria', value: 'Timezone', inputType: 'timezone' },
    {
      label: 'Seleccione los días donde se aplicará la política',
      value: 'ApplyOnDays',
      inputType: 'days',
    },
  ],
};
