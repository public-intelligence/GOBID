import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { Params } from '@angular/router';
import { Observable } from 'rxjs/internal/Observable';
import { Position, ServerResponse } from '../../models';

@Injectable({
  providedIn: 'root',
})
export class PositionService {
  private httpService = inject(GenericHttpService);
  private path = 'position';

  findAll(params?: Params) {
    return this.httpService.findAll(this.path, { params });
  }

  findOne(id: string) : Observable<ServerResponse<Position>> {
    return this.httpService.findOne(`${this.path}/${id}`, {
    });
  }

  search(q: string, params?: Params) {
    return this.httpService.findAll(`${this.path}/search`, {
      params: { q, ...params },
    });
  }
}
