import { inject, Injectable } from '@angular/core';
import {
  OrganizationApplication,
  OrganizationApplicationAdapted,
} from '../models';
import {
  OrganizationAdapter,
  ApplicationAdapter,
  RoleAdapter,
} from '../adapter';

@Injectable({
  providedIn: 'root',
})
export class OrganizationApplicationAdapter {
  private organizationAdapter = inject(OrganizationAdapter);
  private applicationAdapter = inject(ApplicationAdapter);
  private roleAdapter = inject(RoleAdapter);

  adapt(response: OrganizationApplication): OrganizationApplicationAdapted {
    return {
      organizationApplicationId: response.organizationApplicationId ?? '',
      organizationId: response.organizationId ?? '',
      applicationId: response.applicationId ?? '',
      organizationName: response.organizationName ?? '',
      isActive: response.isActive ?? '',
      organization: this.organizationAdapter.adapt(response.organization ?? {}),
      application: this.applicationAdapter.adapt(response.application ?? {}),
      role: (response.role ?? []).map((role) => this.roleAdapter.adapt(role)),
      roleCount: response.roleCount ?? 0,
    };
  }
}
