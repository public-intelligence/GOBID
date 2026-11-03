import { inject, Injectable } from '@angular/core';
import { GenericHttpService } from '../generic-service/generic.service';
import { LegalEntityCreate } from '../../models';
import { Params } from '@angular/router';
import { LegalEntityAdapter } from '../../adapter/legal-entity.adapter';

@Injectable({
  providedIn: 'root',
})
export class LegalEntityService {
  private httpService = inject(GenericHttpService);
  private adapter = inject(LegalEntityAdapter);
  private path = 'identityHub/legalEntity';

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

  update(id: string, data: LegalEntityCreate) {
    return this.httpService.patch(`${this.path}/${id}`, data);
  }

  create(data: LegalEntityCreate) {
    return this.httpService.post(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }

  findByIdentificationNumber(identificationNumber: string) {
    return this.httpService.findOne(
      `${this.path}/identificationNumber/${identificationNumber}`,
      {
        adapter: this.adapter,
      },
    );
  }
}
