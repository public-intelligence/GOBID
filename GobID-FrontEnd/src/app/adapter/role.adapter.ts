import { Injectable } from '@angular/core';
import { Role, RoleAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class RoleAdapter {
  adapt(apiData: Role): RoleAdapted {
    return {
      id: apiData.roleId ?? 0,
      name: apiData.roleName ?? '',
      description: apiData.roleDescription ?? '',
      setAutomaticClaims: apiData.setAutomaticClaims ?? false,
      organizationApplicationId: apiData.organizationApplicationId ?? 0,
      isActive: apiData.isActive ?? false,
      userCount: apiData.userCount ?? 0,
      claimCount: apiData.claimCount ?? 0,
      isSystemRole: apiData.isSystemRole ?? false,
      positionId: apiData.positionId ?? '',
      createdAt: apiData.createdAt ?? '',
      createdBy: apiData.createdBy ?? null,
      updatedAt: apiData.updatedAt ?? null,
      updatedBy: apiData.updatedBy ?? null,
    };
  }
}
