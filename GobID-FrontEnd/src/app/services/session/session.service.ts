import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { QueryParams, SessionFilter } from '../../models';

@Injectable({
  providedIn: 'root',
})
export class SessionService {
  private httpService = inject(GenericHttpService);
  private path = 'Auth/sessions';

  findAll(params?: SessionFilter) {
    return this.httpService.findAll(this.path, { params: params as QueryParams });
  }

  revoke(sessionId: string) {
    return this.httpService.delete(`Auth/sessions/${sessionId}`);
  }

  revokeAll() {
    return this.httpService.delete('Auth/sessions');
  }

  revokeByApplication(applicationId: string) {
    return this.httpService.delete(`Auth/sessions/application/${applicationId}`);
  }
}
