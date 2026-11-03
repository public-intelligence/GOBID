import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { PolicyTypeAdapter } from '../../adapter';
import { Params } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class PolicyTypeService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(PolicyTypeAdapter);
  private path = 'policyType';

  findAll(params?: Params) {
    return this.httpService.findAll(this.path, {
      adapter: this.adapter,
      params,
    });
  }
}
