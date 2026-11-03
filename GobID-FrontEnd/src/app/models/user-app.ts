import {
  OrganizationApplication,
  OrganizationApplicationAdapted,
} from './organization-application';
import { User, UserAdapted } from './user';

export interface UserApp {
  userApplicationId: string;
  userId: string;
  organizationApplicationId: number;
  userApplicationDescription: string;
  isApplicationAdmin: boolean;
  internalAppUserCode: string | null;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string | null;
  updatedBy: string | null;
  rowVersion: string;
  userRole: {
    userRoleId: number;
    roleId: number;
  };
  organizationApplication: OrganizationApplication | null;
  user: User | null;
  policyIds?: string[];
}

export interface UserAppAdapted {
  id: string;
  userId: string;
  organizationApplicationId: number;
  userApplicationDescription: string;
  isApplicationAdmin: boolean;
  internalAppUserCode: string | null;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
  updatedAt: string | null;
  updatedBy: string | null;
  rowVersion: string;
  organizationApplication: OrganizationApplicationAdapted | null;
  user: UserAdapted | null;
  userRoleId: number;
  roleId: number;
  organizationId?: number;
  policyIds?: string[];
}

export interface UserAppCreate {
  userApplicationId: string;
  userId: string;
  organizationApplicationId: number;
  userApplicationDescription: string;
  isApplicationAdmin: boolean;
  isActive: boolean;
  roleId?: number;
  roleName?: string;
  organizationId?: number;
  policyIds?: string[];
}
