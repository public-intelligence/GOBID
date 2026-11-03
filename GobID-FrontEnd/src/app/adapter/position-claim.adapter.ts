import { Injectable } from '@angular/core';
import { PositionClaim, PositionClaimAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class PositionClaimAdapter {
  adapt(apiData: PositionClaim): PositionClaimAdapted {
    return {
      id: apiData.positionClaimId ?? 0,
      positionId: apiData.positionId ?? '',
      claimId: apiData.claimId ?? 0,
      claimName: apiData.claimName ?? '',
      claimCode: apiData.claimCode ?? '',
      isActive: apiData.isActive ?? true,
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
    };
  }
}
