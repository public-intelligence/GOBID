import { inject, Injectable } from '@angular/core';
import { ApplicationAccessAdapter } from '../../adapter';
import { GenericHttpService } from '../generic-service/generic.service';
import {
  ApplicationAccessAdapted,
  ApplicationAccessCreate,
} from '../../models/application-access';

@Injectable({
  providedIn: 'root',
})
export class ApplicationAccessService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(ApplicationAccessAdapter);
  private path = 'applicationaccess';

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  findOneByOrgAppId(id: string) {
    return this.httpService.findAll(
      `${this.path}/?OrganizationApplicationId=${id}`,
      {
        adapter: this.adapter,
      },
    );
  }

  findAll() {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
    });
  }

  upsert(data: ApplicationAccessAdapted | ApplicationAccessCreate) {
    return this.httpService.put(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
