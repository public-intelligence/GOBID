import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { UserApplicationPolicyCreate } from '../../models/policy';
import { UserAppAdapter } from '../../adapter';
import { UserApplicationPolicyAdapter } from '../../adapter/policy.adapter';
import { Params } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class UserAppPolicyService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(UserAppAdapter);
  private policyAdapter = inject(UserApplicationPolicyAdapter);
  private path = 'UserApplicationPolicy';

  findAll(params?: Params) {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
      params,
    });
  }

  findAllByUserApp(userApplicationId: string) {
    return this.httpService.findAll(`${this.path}/${userApplicationId}`, {
      adapter: this.policyAdapter,
    });
  }

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  create(data: UserApplicationPolicyCreate) {
    return this.httpService.post(`${this.path}/assign`, data);
  }

  deactivate(userApplicationPolicyId: string) {
    return this.httpService.delete(`${this.path}/${userApplicationPolicyId}`);
  }
}
