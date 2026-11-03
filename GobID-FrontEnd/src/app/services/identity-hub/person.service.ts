import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, map, throwError } from 'rxjs';
import { GenericHttpService } from '../generic-service/generic.service';
import { PersonCreate, PersonAdapted, Person } from '../../models/person';
import { ServerResponse } from '../../models/global';
import { PersonAdapter } from '../../adapter/person.adapter';
import { Params } from '@angular/router';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class PersonService {
  private httpService = inject(GenericHttpService);
  private http = inject(HttpClient);
  private adapter = inject(PersonAdapter);
  private path = 'identityHub/Person';
  private mainUrl = environment.apiUrl;

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

  update(id: string, data: Partial<PersonCreate>) {
    return this.httpService.patch(`${this.path}/${id}`, data);
  }

  create(data: PersonCreate) {
    return this.httpService.post(this.path, data);
  }

  delete(id: string) {
    return this.httpService.delete(`${this.path}/${id}`);
  }

  findByIdentificationNumber(identificationNumber: string) {
    return this.httpService.findOne(
      `${this.path}/cedula/${identificationNumber}`,
      {
        adapter: this.adapter,
      },
    );
  }

  findByIdentificationNumberPublic(
    identificationNumber: string,
    isPassport: boolean,
  ) {
    return this.http
      .get<
        ServerResponse<Person>
      >(`${this.mainUrl}/public/identityHub/Person/cedula/${identificationNumber}?isPassport=${isPassport}`)
      .pipe(
        map((response) => this.adapter.adapt(response.data)),
        catchError((error: HttpErrorResponse) =>
          throwError(() => error?.error),
        ),
      );
  }

  getPhotobyPersonId(personId: string) {
    return this.http.get(`${this.mainUrl}/${this.path}/${personId}/photo`, {
      responseType: 'blob',
    });
  }

  updatePhotobyPersonId(personId: string, photo: File) {
    const formData = new FormData();
    formData.append('file', photo);
    return this.http.put(
      `${this.mainUrl}/${this.path}/${personId}/photo`,
      formData,
    );
  }
}
