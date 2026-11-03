import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { CreateRoleClaim, QueryParams } from '../../models';
import { RoleClaimAdapter } from '../../adapter';

@Injectable({
  providedIn: 'root',
})
export class RoleClaimService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(RoleClaimAdapter);
  private path = 'RoleClaim';

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

  create(data: CreateRoleClaim[]) {
    return this.httpService.put(this.path, data);
  }

  delete(id: number) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
