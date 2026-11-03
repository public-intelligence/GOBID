import { Injectable } from '@angular/core';
import { Application, ApplicationAdapted } from '../models';

@Injectable({
  providedIn: 'root',
})
export class ApplicationAdapter {
  adapt(apiData: Application): ApplicationAdapted {
    return {
      id: apiData.applicationId ?? '',
      name: apiData.applicationName ?? '',
      code: apiData.code ?? '',
      redirectUrl: apiData.redirectUrl ?? '',
      description: apiData.applicationDescription ?? '',
      isActive: apiData.isActive ?? '',
      isPublic: apiData.isPublic ?? false,
      createdAt: apiData.createdAt ?? '',
      updatedAt: apiData.updatedAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedBy: apiData.updatedBy ?? '',
    };
  }
}
