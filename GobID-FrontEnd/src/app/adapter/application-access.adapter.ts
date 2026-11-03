import { Injectable } from '@angular/core';
import { ApplicationAccess, ApplicationAccessAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class ApplicationAccessAdapter {
  adapt(apiData: ApplicationAccess): ApplicationAccessAdapted {
    return {
      applicationAccessId: apiData.applicationAccessId ?? '',
      organizationApplicationId: apiData.organizationApplicationId ?? '',
      accessTypeId: apiData.accessTypeId ?? '',
      configurationJson: apiData.configurationJson ?? '',
      isActive: apiData.isActive ?? false,
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedBy: apiData.updatedBy ?? '',
    };
  }
}
