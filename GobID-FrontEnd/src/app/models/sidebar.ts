import { IconProp } from '@fortawesome/fontawesome-svg-core';

export interface SideElement {
  id: number;
  title: string;
  path: string;
  permissions: string[];
}

export interface SideData {
  id: number;
  title: string;
  icon: IconProp | string;
  path?: string;
  withElements?: boolean;
  collapse?: boolean;
  elements?: SideElement[];
  permissions: string[];
}
