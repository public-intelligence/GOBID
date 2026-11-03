import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import {
  AppCredential,
  AppCredentialCreated,
  CreateAppCredentialRequest,
  ServerResponse,
} from '../../models';

@Injectable({
  providedIn: 'root',
})
export class AppCredentialService {
  private httpService = inject(GenericHttpService);

  list(applicationId: string) {
    return this.httpService.findAll<AppCredential, AppCredential>(
      `applications/${applicationId}/credentials`,
    );
  }

  create(applicationId: string, body: CreateAppCredentialRequest) {
    return this.httpService.post<ServerResponse<AppCredentialCreated>>(
      `applications/${applicationId}/credentials`,
      body,
    );
  }

  revoke(applicationId: string, credentialId: string) {
    return this.httpService.delete(
      `applications/${applicationId}/credentials/${credentialId}`,
    );
  }
}
