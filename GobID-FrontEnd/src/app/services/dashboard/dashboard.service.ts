import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { DashboarStatsAdapter } from '../../adapter/dashboard.adapter';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(DashboarStatsAdapter);

  getMetrics() {
    return this.httpService.findOne('dashboard/metrics', {
      adapter: this.adapter,
    });
  }

  getChart() {
    return this.httpService.findAll('dashboard/registrations');
  }
}
