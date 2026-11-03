import { IconProp } from '@fortawesome/fontawesome-svg-core';

export interface Breadcrumb {
  label: string;
  path: string;
  active: boolean;
  icon?: IconProp | string;
}
