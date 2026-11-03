import { Injectable } from '@angular/core';
import {
  OrganizationContact,
  OrganizationContactAdapted,
} from '../models/index';

@Injectable({
  providedIn: 'root',
})
export class OrganizationContactAdapter {
  adapt(apiData: OrganizationContact): OrganizationContactAdapted {
    return {
      contactId: apiData.contactId ?? '',
      organizationId: apiData.organizationId ?? '',
      organizationContactName: apiData.organizationContactName ?? '',
      position: apiData.position ?? '',
      email: apiData.email ?? '',
      phone: apiData.phone ?? '',
      isActive: apiData.isActive ?? false,
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
    };
  }
}
