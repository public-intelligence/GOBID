import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { PersonnelAdapter } from '../../adapter';
import { PersonnelUpsert } from '../../models';
import { Params } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class PersonnelService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(PersonnelAdapter);
  private path = 'personnel';

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  findAllByOrganization(organizationId: number, params?: Params) {
    return this.httpService.findAll(
      `${this.path}/organization/${organizationId}`,
      { adapter: this.adapter, params },
    );
  }

  upsert(data: PersonnelUpsert) {
    return this.httpService.put(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
