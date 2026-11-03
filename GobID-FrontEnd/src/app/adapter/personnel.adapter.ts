import { Injectable } from '@angular/core';
import { Personnel, PersonnelAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class PersonnelAdapter {
  adapt(apiData: Personnel): PersonnelAdapted {
    return {
      id: apiData.personnelId ?? '',
      personnelId: apiData.personnelId ?? '',
      positionId: apiData.positionId ?? '',
      positionName: apiData.position?.name ?? '',
      positionNominateId: apiData.positionNominateId ?? '',
      localityId: apiData.localityId ?? '',
      localityName: apiData.locality?.nombreOficial ?? '',
      organizationId: apiData.organizationId ?? 0,
      person: apiData.person,
      isActive: apiData.isActive ?? true,
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
    };
  }
}
