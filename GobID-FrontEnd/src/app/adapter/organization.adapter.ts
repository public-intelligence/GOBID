import { inject, Injectable } from '@angular/core';
import { Organization, OrganizationAdapted } from '../models/organization';
import { OrganizationContactAdapter } from './index';

@Injectable({
  providedIn: 'root',
})
export class OrganizationAdapter {
  organizationContactsAdapter = inject(OrganizationContactAdapter);
  adapt(apiData: Organization): OrganizationAdapted {
    return {
      id: apiData.organizationId ?? '',
      legalEntityId: apiData.legalEntityId ?? '',
      name: apiData.organizationName ?? '',
      acronym: apiData.acronym ?? '',
      description: apiData.organizationDescription ?? '',
      slogan: apiData.slogan ?? '',
      website: apiData.website ?? '',
      email: apiData.email ?? '',
      organizationAddress: apiData.organizationAddress ?? '',
      contact: apiData.contact?.map((item) => ({
        contactId: item.contactId ?? 0,
        organizationId: item.organizationId ?? 0,
        organizationContactName: item.organizationContactName ?? '',
        position: item.position ?? '',
        email: item.email ?? '',
        phone: item.phone ?? '',
        isActive: item.isActive ?? false,
        createdAt: item.createdAt ?? '',
        updatedAt: item.updatedAt ?? '',
      })),
      phone: apiData.phone ?? '',
      isActive: apiData.isActive ?? false,
      rowVersion: apiData.rowVersion ?? '',
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedBy: apiData.updatedBy ?? '',
      count: apiData.count ?? 0,
      legalEntity: apiData.legalEntity ?? undefined,
    };
  }
}
