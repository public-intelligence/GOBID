import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { UserCreate, UserStatus } from '../../models';
import { UserAdapter } from '../../adapter';
import { Params } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(UserAdapter);
  private path = 'user';

  findAll(params?: Params) {
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

  create(data: UserCreate) {
    return this.httpService.put(this.path, data);
  }

  update(id: string, data: Partial<UserCreate>) {
    return this.httpService.put(`${this.path}`, data);
  }

  updateStatus(data: UserStatus) {
    return this.httpService.put(`${this.path}/status`, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }
}
