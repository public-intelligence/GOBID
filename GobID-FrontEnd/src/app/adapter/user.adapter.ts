import { Injectable } from '@angular/core';
import { User, UserAdapted } from '../models';
import { PersonAdapter } from './person.adapter';

@Injectable({
  providedIn: 'root',
})
export class UserAdapter {
  personAdapter = new PersonAdapter();
  adapt(apiData: User): UserAdapted {
    return {
      id: apiData.userId ?? '',
      userId: apiData.userId ?? '',
      personnelId: apiData.personnelId ?? '',
      email: apiData.email ?? '',
      position: apiData.position ?? '',
      positionId: apiData.positionId ?? '',
      personPositionId: apiData.personPositionId ?? '',
      userPassword: apiData.userPassword ?? '',
      securityKey: apiData.securityKey ?? '',
      isServerAdmin: apiData.isServerAdmin ?? false,
      isDomainAuthenticated: apiData.isDomainAuthenticated ?? false,
      mustChangePassword: apiData.mustChangePassword ?? false,
      personnel: this.personAdapter.adapt(apiData.personnel ?? {}),
      admissionDate: apiData.admissionDate ?? '',
      dischargeDate: apiData.dischargeDate ?? '',
      count: apiData.count ?? 0,
      organizationId: apiData.organizationId ?? '',
      organizationName: apiData.organizationName ?? '',
      isActive: apiData.isActive ?? false,
      createdAt: apiData.createdAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedAt: apiData.updatedAt ?? '',
      updatedBy: apiData.updatedBy ?? '',
      rowVersion: apiData.rowVersion ?? '',
      userApplications:
        apiData.userApplications?.map((app) => ({
          organizationApplicationId: app.organizationApplicationId ?? 0,
          roleId: app.roleId ?? 0,
          roleName: app.roleName ?? '',
          isActive: app.isActive,
          organizationId: app.organizationId ?? 0,
          userApplicationId: app.userApplicationId ?? '',
        })) || [],
    };
  }
}
