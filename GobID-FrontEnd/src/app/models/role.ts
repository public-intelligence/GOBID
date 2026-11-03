export interface Role {
  roleId: number;
  roleName: string;
  roleDescription: string;
  setAutomaticClaims: boolean;
  organizationApplicationId: number;
  isActive: boolean;
  userCount?: number;
  claimCount?: number;
  isSystemRole?: boolean;
  positionId?: string;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}

export interface RoleAdapted {
  id: number;
  name: string;
  description: string;
  setAutomaticClaims: boolean;
  organizationApplicationId: number;
  userCount?: number;
  claimCount?: number;
  isSystemRole?: boolean;
  isActive: boolean;
  positionId?: string;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
}

export interface CreateRole {
  roleId: number;
  roleName: string;
  roleDescription: string;
  setAutomaticClaims: boolean;
  organizationApplicationId: number;
  isActive: boolean;
  isSystemRole: boolean;
  positionId?: string;
}
