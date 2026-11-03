import { Injectable } from '@angular/core';
import { Person, PersonAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class PersonAdapter {
  adapt(apiData: Person): PersonAdapted {
    return {
      id: apiData.id ?? apiData.id ?? '',
      firstName: apiData.firstName ?? apiData.firstName ?? '',
      lastName: apiData.lastName ?? apiData.lastName ?? '',
      identificationNumber:
        apiData.identificationNumber ?? apiData.identificationNumber ?? '',
      gender: apiData.gender ?? '',
      email: apiData.email ?? '',
      phoneNumber: apiData.phoneNumber ?? '',
      isExternal: apiData.isExternal ?? false,
      status: apiData.status ?? 0,
      position: apiData.position ?? undefined,
      positionId: apiData.position?.id ?? '',
      organizationalStructure: apiData.organizationalStructure ?? undefined,
      organizationId: apiData.organizationId ?? '',
      personnelId: apiData.personnelId ?? '',
    };
  }
}
