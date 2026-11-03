import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { Params } from '@angular/router';
import { PolicyAdapted } from '../../models';
import { PolicyAdapter } from '../../adapter';

@Injectable({
  providedIn: 'root',
})
export class PolicyService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(PolicyAdapter);
  private path = 'policy';

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  findAll(params?: Params) {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
      params,
    });
  }

  findAllByOrganization(organizationId: string, params?: Params) {
    return this.httpService.findAll(
      `${this.path}/organization/${organizationId}`,
      { adapter: this.adapter, params },
    );
  }

  upsert(data: PolicyAdapted) {
    return this.httpService.put(`${this.path}`, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
