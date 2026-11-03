import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { OrganizationApplicationAdapter } from '../../adapter';
import { OrganizationApplicationCreate } from '../../models';
import { Params } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class OrganizationApplicationService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(OrganizationApplicationAdapter);
  private path = 'organizationapplication';

  findAll() {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
    });
  }

  findAllByOrganizationId(organizationId: string, params?: Params) {
    return this.httpService.findAll(
      `${this.path}/organization/${organizationId}`,
      {
        adapter: this.adapter,
        params,
      },
    );
  }

  findAllByToken(params?: Params) {
    return this.httpService.findAll(`${this.path}/organization`, {
      adapter: this.adapter,
      params,
    });
  }

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  findAllByApplicationOrganizationId(idOrg: string, idApp: string) {
    return this.httpService.findAll(
      `${this.path}/?OrganizationId=${idOrg}&ApplicationId=${idApp}`,
      {
        adapter: this.adapter,
      },
    );
  }
  findAllByApplicationOrganizationByApplicationId(idApp: string) {
    return this.httpService.findAll(
      `${this.path}/?ApplicationId=${idApp}`,
      {
        adapter: this.adapter,
      },
    );
  }

  findAllApplicationByToken(params?: Params) {
    return this.httpService.findAll(`${this.path}/Organization`, {
      adapter: this.adapter,
      params,
    });
  }

  upsert(data: OrganizationApplicationCreate) {
    return this.httpService.put(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
