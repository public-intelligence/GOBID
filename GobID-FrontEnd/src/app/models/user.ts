import { Person, PersonAdapted } from './person';
import { UserAppCreate } from './user-app';

export interface User {
  id: string;
  userId: string;
  personnelId: string;
  organizationId: string;
  organizationName: string;
  email: string;
  positionId: string;
  personPositionId: string;
  position: string;
  userPassword: string;
  securityKey: string;
  isServerAdmin: boolean;
  isDomainAuthenticated: boolean;
  mustChangePassword: boolean;
  personnel: Person;
  admissionDate: string;
  dischargeDate: string;
  count: number;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string;
  updatedBy: string;
  rowVersion: string;
  userApplications?: Pick<
    UserAppCreate,
    | 'organizationApplicationId'
    | 'roleId'
    | 'roleName'
    | 'isActive'
    | 'organizationId'
    | 'userApplicationId'
    | 'policyIds'
  >[];
}

export interface UserAdapted {
  id: string;
  userId: string;
  personnelId: string;
  organizationId: string;
  organizationName: string;
  email: string;
  personPositionId: string;
  position: string;
  positionId: string;
  userPassword: string;
  securityKey: string;
  isServerAdmin: boolean;
  isDomainAuthenticated: boolean;
  mustChangePassword: boolean;
  personnel: PersonAdapted;
  admissionDate: string;
  dischargeDate: string;
  count: number;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string;
  updatedBy: string;
  rowVersion: string;
  userApplications?: Pick<
    UserAppCreate,
    | 'organizationApplicationId'
    | 'roleId'
    | 'roleName'
    | 'isActive'
    | 'organizationId'
    | 'policyIds'
    | 'userApplicationId'
  >[];
}

export interface UserCreate {
  userId: string;
  personnelId: string;
  organizationId: string;
  email: string;
  personPositionId: string;
  position?: string;
  positionId: string;
  userPassword: string;
  securityKey: string;
  isServerAdmin: boolean;
  isDomainAuthenticated: boolean;
  mustChangePassword: boolean;
  isActive: boolean;
  userApplications: Pick<
    UserAppCreate,
    | 'organizationApplicationId'
    | 'roleId'
    | 'roleName'
    | 'isActive'
    | 'policyIds'
  >[];
}

export interface UserStatus {
  userId: string;
  isActive: boolean;
}
