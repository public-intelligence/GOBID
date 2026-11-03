import { inject, Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import {
  HttpClient,
  HttpErrorResponse,
  HttpParams,
  HttpResponse,
} from '@angular/common/http';
import { catchError, map, Observable, throwError } from 'rxjs';
import { PaginationResponse, QueryParams, ServerResponse } from '../../models';

/**
 * @Response Modelo de respuesta de la API
 * @Adapted Modelo para la adaptación (Modelo visualizado)
 */
export interface Adapter<Response, Adapted> {
  adapt(item: Response): Adapted;
}

@Injectable({
  providedIn: 'root',
})
export class GenericHttpService {
  private mainUrl = environment.apiUrl;
  private http = inject(HttpClient);

  /**
   * Obtiene una lista paginada de recursos
   * @param params Parámetros de paginación y búsqueda
   * @param adapter Adaptador para transformar la respuesta
   * @returns Observable con la respuesta paginada
   */
  /*  findAll<Response, Adapted>(
    path: string,
    config: { params: QueryParams; adapter: Adapter<Response, Adapted> },
  ): Observable<PaginationResponse<Adapted>> {
    const url = `${this.mainUrl}/${path}`;

    const httpParams = this.buildPaginationParams(config.params);

    return this.http
      .get<
        ServerResponse<Response[]>
      >(url, { params: httpParams, observe: 'response' })
      .pipe(
        map((response: HttpResponse<ServerResponse<Response[]>>) =>
          this.handlePaginationResponse<Response, Adapted>(
            config.adapter,
            response,
          ),
        ),
        catchError(this.handleError),
      );
  } */

  /**
   * Obtiene una de recursos
   * @param adapter Adaptador para transformar la respuesta
   * @returns Observable con la respuesta
   */
  findAll<Response, Adapted>(
    path: string,
    config?: {
      params?: QueryParams;
      search?: string;
      adapter?: Adapter<Response, Adapted>;
    },
  ): Observable<ServerResponse<Adapted[]>> {
    const url = `${this.mainUrl}/${path}`;

    const httpParams = this.buildPaginationParams(config?.params || {});

    return this.http
      .get<
        ServerResponse<Response[]>
      >(url, { params: httpParams, observe: 'response' })
      .pipe(
        map((response: HttpResponse<ServerResponse<Response[]>>) => {
          if (!response.body) {
            return {
              data: [],
              message: '',
              errors: '',
              succeeded: false,
              pageNumber: 0,
              pageSize: 0,
              totalCount: 0,
              totalPages: 0,
            };
          }

          return {
            ...response.body,
            data: config?.adapter
              ? response.body.data.map((item: Response) =>
                  config.adapter!.adapt(item),
                )
              : (response.body.data as unknown as Adapted[]),
          };
        }),
        catchError(this.handleError),
      );
  }

  /**
   * Obtiene una de recursos
   * @param adapter Adaptador para transformar la respuesta
   * @returns Observable con la respuesta
   */
  findAllByAd<Response, Adapted>(
    path: string,
    id: string,
    config?: {
      params?: QueryParams;
      search?: string;
      adapter?: Adapter<Response, Adapted>;
    },
  ): Observable<Adapted[]> {
    const url = `${this.mainUrl}/${path}/${id}`;

    const httpParams = this.buildPaginationParams(config?.params || {});

    return this.http
      .get<
        ServerResponse<Response[]>
      >(url, { params: httpParams, observe: 'response' })
      .pipe(
        map((response: HttpResponse<ServerResponse<Response[]>>) => {
          if (!response.body) return [];
          if (config?.adapter) {
            return response.body.data.map((item: Response) =>
              config.adapter!.adapt(item),
            );
          }
          // If no adapter, cast each item to Adapted
          return response.body.data.map(
            (item: Response) => item as unknown as Adapted,
          );
        }),
        catchError(this.handleError),
      );
  }

  /**
   * Obtiene un recurso por ID
   * @param path Ruta del recurso
   * @param adapter Adaptador para transformar la respuesta
   */
  findOne<Response, Adapted>(
    path: string,
    config?: { adapter?: Adapter<Response, Adapted> },
  ): Observable<Adapted> {
    const url = `${this.mainUrl}/${path}`;
    return this.http.get<ServerResponse<Response>>(url).pipe(
      map((item) => {
        if (config?.adapter) {
          return config.adapter.adapt(item.data);
        }
        return item as unknown as Adapted;
      }),
      catchError(this.handleError),
    );
  }

  /**
   * Crea un nuevo recurso
   * @param path Ruta del recurso
   * @param data Datos del recurso a crear
   */
  post<Response>(path: string, data: unknown): Observable<Response> {
    const url = `${this.mainUrl}/${path}`;
    return this.http
      .post<Response>(url, data)
      .pipe(catchError(this.handleError));
  }

  /**
   * Actualiza un recurso existente
   * @param path Ruta del recurso
   * @param data Datos actualizados
   */
  put<Response>(path: string, data: unknown): Observable<Response> {
    const url = `${this.mainUrl}/${path}`;
    return this.http
      .put<Response>(url, data)
      .pipe(catchError(this.handleError));
  }

  /**
   * Actualiza un recurso existente
   * @param path Ruta del recurso
   * @param data Datos actualizados
   */
  patch<Response>(path: string, data: unknown): Observable<Response> {
    const url = `${this.mainUrl}/${path}`;
    return this.http
      .patch<Response>(url, data)
      .pipe(catchError(this.handleError));
  }

  /**
   * Elimina un recurso
   * @param path Ruta del recurso
   */
  delete(path: string): Observable<void> {
    const url = `${this.mainUrl}/${path}`;
    return this.http.delete<void>(url).pipe(catchError(this.handleError));
  }

  /**
   * Construye los parámetros de paginación para la solicitud HTTP
   * @param params Parámetros de paginación
   * @returns HttpParams con los parámetros de paginación
   */
  private buildPaginationParams(params: QueryParams): HttpParams {
    let httpParams = new HttpParams();
    // .set('pageNumber', params.pageNumber ? params.pageNumber + 1 : 1)
    // .set('pageSize', params.pageSize || 10);
    if (params) {
      const paramsKey = Object.keys(params);
      paramsKey.forEach((param) => {
        if (
          params[param.toString()] === undefined ||
          params[param.toString()] === null
        )
          return;
        httpParams = httpParams.set(param, params[param.toString()] ?? '');
      });
    }

    return httpParams;
  }

  /**
   * Obtiene la respuesta paginación de la API y la adapta a la respuesta esperada
   * @param response Respuesta de la API
   */
  private handlePaginationResponse<Response, Adapted>(
    adapter: Adapter<Response, Adapted>,
    response: HttpResponse<ServerResponse<Response[]>>,
  ): PaginationResponse<Adapted> {
    return {
      pageNumber: Number(response.headers.get('x-current-page')),
      pageSize: Number(response.headers.get('x-page-size')),
      totalRecords: Number(response.headers.get('x-total')),
      totalPages: Number(response.headers.get('x-page-count')),
      results:
        response.body && response.body.data
          ? response.body.data.map((item: Response) => adapter.adapt(item))
          : [],
    };
  }

  /**
   * Maneja los errores de la API
   */
  private handleError(error: HttpErrorResponse) {
    return throwError(() => error?.error);
  }
}
