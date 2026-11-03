import { Injectable } from '@angular/core';
import { AccessType, AccessTypeAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class AccessTypeAdapter {
  adapt(apiData: AccessType): AccessTypeAdapted {
    return {
      id: apiData.accessTypeId ?? '',
      name: apiData.accessTypeName ?? '',
      description: apiData.accessTypeDescription ?? '',
      isActive: apiData.isActive ?? '',
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedBy: apiData.updatedBy ?? '',
    };
  }
}
