import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { CreateRole, QueryParams } from '../../models';
import { RoleAdapter } from '../../adapter';

@Injectable({
  providedIn: 'root',
})
export class RoleService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(RoleAdapter);
  private path = 'role';

  findAll(params?: QueryParams) {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
      params,
    });
  }

  findOne(id: number) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  create(data: CreateRole) {
    return this.httpService.put(this.path, data);
  }

  delete(id: number) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
