export interface RoleClaim {
  roleClaimId: number;
  roleId: number;
  claimId: number;
  claimName: string;
  isActive: boolean;
  createdBy: string;
  updatedBy: string;
}

export interface RoleClaimAdapted {
  id: number;
  roleId: number;
  claimId: number;
  claimName: string;
  isActive: boolean;
  createdBy: string;
  updatedBy: string;
}

export interface CreateRoleClaim {
  roleId: number;
  claimId: number;
  isActive: boolean;
}
