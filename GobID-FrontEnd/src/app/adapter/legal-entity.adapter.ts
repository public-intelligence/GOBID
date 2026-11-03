import { Injectable } from '@angular/core';
import { LegalEntity, LegalEntityAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class LegalEntityAdapter {
  adapt(apiData: LegalEntity): LegalEntityAdapted {
    return {
      id: apiData.id ?? '',
      legalName: apiData.legalName ?? '',
      socialName: apiData.socialName ?? '',
      identificationNumber: apiData.identificationNumber ?? '',
      email: apiData.email ?? '',
      phoneNumber: apiData.phoneNumber ?? '',
      type: apiData.type ?? '',
      status: apiData.status ?? '',
    };
  }
}
