import { Injectable } from '@angular/core';
import { RoleClaim, RoleClaimAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class RoleClaimAdapter {
  adapt(apiData: RoleClaim): RoleClaimAdapted {
    return {
      id: apiData.roleClaimId ?? 0,
      roleId: apiData.roleId ?? 0,
      claimId: apiData.claimId ?? 0,
      claimName: apiData.claimName ?? '',
      isActive: apiData.isActive ?? false,
      createdBy: apiData.createdBy ?? null,
      updatedBy: apiData.updatedBy ?? null,
    };
  }
}
