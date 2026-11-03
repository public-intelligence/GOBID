import { inject, Injectable } from '@angular/core';
import {
  OrganizationApplication,
  User,
  UserApp,
  UserAppAdapted,
} from '../models';
import { OrganizationApplicationAdapter } from './organization-application.adapter';
import { UserAdapter } from './user.adapter';

@Injectable({
  providedIn: 'root',
})
export class UserAppAdapter {
  private organizationApplication = inject(OrganizationApplicationAdapter);
  private user = inject(UserAdapter);
  adapt(apiData: UserApp): UserAppAdapted {
    return {
      id: apiData.userApplicationId ?? '',
      userId: apiData.userId ?? '',
      organizationApplicationId: apiData.organizationApplicationId ?? 0,
      userApplicationDescription: apiData.userApplicationDescription ?? '',
      isApplicationAdmin: apiData.isApplicationAdmin ?? false,
      internalAppUserCode: apiData.internalAppUserCode ?? null,
      isActive: apiData.isActive ?? false,
      organizationId: apiData.organizationApplication?.organizationId ?? 0,
      createdAt: apiData.createdAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedAt: apiData.updatedAt ?? null,
      updatedBy: apiData.updatedBy ?? null,
      rowVersion: apiData.rowVersion ?? '',
      organizationApplication:
        this.organizationApplication.adapt(
          apiData.organizationApplication ?? ({} as OrganizationApplication),
        ) ?? null,
      user: this.user.adapt(apiData.user ?? ({} as User)) ?? null,
      userRoleId: apiData.userRole?.userRoleId ?? 0,
      roleId: apiData.userRole?.roleId ?? 0,
      policyIds: apiData.policyIds ?? [],
    };
  }
}
