import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { AccessTypeAdapter } from '../../adapter';
import { AccessTypeAdapted } from '../../models';

@Injectable({
  providedIn: 'root',
})
export class accessTypeService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(AccessTypeAdapter);
  private path = 'accesstype';

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  findAll() {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
    });
  }

  upsert(data: AccessTypeAdapted) {
    return this.httpService.put<AccessTypeAdapted>(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
