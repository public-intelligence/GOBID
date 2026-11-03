import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { ClaimCreate, QueryParams } from '../../models';
import { ClaimAdapter } from '../../adapter';

@Injectable({
  providedIn: 'root',
})
export class ClaimService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(ClaimAdapter);
  private path = 'claim';

  findAll(params?: QueryParams) {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
      params,
    });
  }

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  create(data: ClaimCreate[]) {
    return this.httpService.put(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
