import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { ApplicationCreate } from '../../models';
import { ApplicationAdapter } from '../../adapter/application.adapter';
import { Params } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class ApplicationService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(ApplicationAdapter);
  private path = 'application';

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

  findPublic(id: string) {
    return this.httpService.findOne(`public/application/${id}`, {
      adapter: this.adapter,
    });
  }

  update(data: ApplicationCreate) {
    return this.httpService.put(this.path, data);
  }

  create(data: ApplicationCreate) {
    return this.httpService.put(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }

  duplicate(id: string) {
    return this.httpService.post(`${this.path}/${id}/duplicate`, {});
  }
}
