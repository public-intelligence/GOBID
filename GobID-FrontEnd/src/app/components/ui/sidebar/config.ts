import { faBuilding } from '@fortawesome/free-regular-svg-icons';
import { SideData } from '../../../models/sidebar';
import {
  faBorderAll,
  faHome,
  faLink,
  faUser,
  faIdCard,
  faClockRotateLeft,
  faLock,
} from '@fortawesome/free-solid-svg-icons';

export const sideData: SideData[] = [
  {
    id: 1,
    title: 'Dashboard',
    icon: faHome,
    path: '/dashboard',
    permissions: ['view_views'],
  },
  {
    id: 2,
    title: 'Organizaciones',
    icon: faBuilding,
    permissions: ['view_org'],
    path: '/organizations',
  },
  {
    id: 3,
    title: 'Aplicaciones',
    icon: faBorderAll,
    path: '/applications',
    permissions: ['view_app'],
  },
  {
    id: 4,
    title: 'Usuarios',
    icon: faUser,
    path: '/users',
    permissions: ['view_user'],
  },
  {
    id: 5,
    title: 'Sesiones',
    icon: faClockRotateLeft,
    path: '/sessions',
    permissions: ['super_admin'],
  },
  {
    id: 6,
    title: 'Políticas',
    icon: faLock,
    path: '/policy',
    permissions: ['view_policy'],
  },
  {
    id: 7,
    title: 'Vinculación',
    icon: faLink,
    withElements: true,
    collapse: true,
    permissions: ['view_views'],
    elements: [
      {
        id: 1,
        title: 'Org-App',
        path: '/organization-application',
        permissions: ['view_org_app'],
      },
      {
        id: 2,
        title: 'Roles-App',
        path: '/role-application',
        permissions: ['view_user'],
      },
    ],
  },
  {
    id: 8,
    title: 'Identidad',
    icon: faIdCard,
    withElements: true,
    collapse: true,
    permissions: ['view_identity_hub'],
    elements: [
      {
        id: 1,
        title: 'Persona',
        path: '/identity-hub/person',
        permissions: ['view_person'],
      },
      {
        id: 2,
        title: 'Jurídica',
        path: '/identity-hub/legal-entity',
        permissions: ['view_le'],
      },
    ],
  },
];
