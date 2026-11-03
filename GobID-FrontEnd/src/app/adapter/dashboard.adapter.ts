import { Injectable } from '@angular/core';
import { DashboardStats } from '../models';

@Injectable({
  providedIn: 'root',
})
export class DashboarStatsAdapter {
  adapt(apiData: DashboardStats): DashboardStats {
    return {
      totalUsers: apiData.totalUsers ?? '',
      totalOrganizations: apiData.totalOrganizations ?? 0,
      totalApplications: apiData.totalApplications ?? '',
    };
  }
}
