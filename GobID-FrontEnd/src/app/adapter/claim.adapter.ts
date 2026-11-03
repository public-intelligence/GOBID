import { Injectable } from '@angular/core';
import { Claim, ClaimAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class ClaimAdapter {
  adapt(apiData: Claim): ClaimAdapted {
    return {
      id: apiData.claimId ?? '',
      applicationId: apiData.applicationId ?? '',
      name: apiData.claimName ?? '',
      code: apiData.code ?? '',
      description: apiData.claimDescription ?? '',
      isActive: apiData.isActive ?? '',
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedBy: apiData.updatedBy ?? '',
      isAutomatic: apiData.isAutomatic ?? false,
    };
  }
}
