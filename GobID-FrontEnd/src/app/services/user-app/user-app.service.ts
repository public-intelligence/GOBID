import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { UserAppCreate } from '../../models';
import { UserAdapter, UserAppAdapter } from '../../adapter';

@Injectable({
  providedIn: 'root',
})
export class UserAppService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(UserAppAdapter);
  private adapterUser = inject(UserAdapter);
  private path = 'userapplication';

  findAllUserApplicationsCount() {
    return this.httpService.findAll(`user/count`, {
      adapter: this.adapterUser,
    });
  }

  findAll(userId?: string) {
    return this.httpService.findAll(
      `${this.path}/?pageNumber=1&pageSize=50&userId=${userId}`,
      {
        adapter: this.adapter,
      },
    );
  }

  findOne(id: string) {
    return this.httpService.findOne(`${this.path}/${id}`, {
      adapter: this.adapter,
    });
  }

  create(data: UserAppCreate) {
    return this.httpService.put(this.path, data);
  }
}
