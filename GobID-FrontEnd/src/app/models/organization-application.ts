import { Application, ApplicationAdapted } from './application';
import { Organization, OrganizationAdapted } from './organization';
import { Role, RoleAdapted } from './role';

export interface OrganizationApplication {
  organizationApplicationId: number;
  organizationId: number;
  applicationId: number;
  organizationName?: string;
  isActive: boolean;
  organization: Organization;
  application: Application;
  role: Role[];
  roleCount?: number;
}

export interface OrganizationApplicationAdapted {
  organizationApplicationId: number;
  organizationId: number;
  applicationId: number;
  organizationName?: string;
  isActive: boolean;
  organization: OrganizationAdapted;
  application: ApplicationAdapted;
  role: RoleAdapted[];
  roleCount?: number;
}

export interface OrganizationApplicationCreate {
  organizationApplicationId?: number;
  organizationId: number;
  applicationId: number;
  isActive: boolean;
}
