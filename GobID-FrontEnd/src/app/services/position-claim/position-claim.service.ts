import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { PositionClaimAdapter } from '../../adapter';
import { CreatePositionClaim, QueryParams } from '../../models';

@Injectable({
  providedIn: 'root',
})
export class PositionClaimService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(PositionClaimAdapter);
  private path = 'positionclaim';

  findAll(params?: QueryParams) {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
      params,
    });
  }

  create(data: CreatePositionClaim) {
    return this.httpService.put(this.path, data);
  }

  delete(id: number) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
