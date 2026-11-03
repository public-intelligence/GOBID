import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import {
  OrganizationContactAdapted,
  OrganizationContactUpsert,
} from '../../models/index';
import { Params } from '@angular/router';
import { OrganizationContactAdapter } from '../../adapter/organization-contact.adapter';

@Injectable({
  providedIn: 'root',
})
export class OrganizationContactService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(OrganizationContactAdapter);
  private path = 'organization/contacts';

  findAllById(id: string) {
    return this.httpService.findAllByAd(`${this.path}`, id, {
      adapter: this.adapter,
    });
  }

  findAll(params?: Params) {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
      params,
    });
  }

  upsert(data: OrganizationContactUpsert[] | OrganizationContactAdapted[]) {
    return this.httpService.put(`${this.path}`, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
