import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { OrganizationAdapter } from '../../adapter/organization.adapter';
import {
  OrganizationAdapted,
  OrganizationCreate,
} from '../../models/organization';
import { Params } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class OrganizationService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(OrganizationAdapter);
  private path = 'organization';

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

  upsert(data: OrganizationCreate | OrganizationAdapted) {
    return this.httpService.put(`${this.path}`, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }

  locality(orgId: string | number, q: string, params?: Params) {
    return this.httpService.findAll(`${this.path}/${orgId}/locality`, {
      params: { nombre: q, ...params },
    });
  }
}
