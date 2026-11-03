import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { catchError, map, Observable, tap, throwError } from 'rxjs';
import {
  AuthRequest,
  AuthResponse,
  ChangePassword,
  ForgotPassword,
  ResetPassword,
  AuthTokenResponse,
  RefreshTokenResponse,
  RegisterUser,
  RegisterUserResponse,
  PersonUserCreate,
} from '../../models';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { ErrorResponse } from '../../models';
import { TokenDecoder } from '../token-decoder/token-decoder';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private mainUrl = environment.apiUrl;
  private gobIdLogin = environment.appUrl;
  private tokenDecoder = inject(TokenDecoder);

  http = inject(HttpClient);
  router = inject(Router);

  /**
   * Realiza el inicio de sesión del usuario.
   * @param identificationNumber Identificación del usuario.
   * @param password Contraseña del usuario.
   */
  onLogin(credentials: AuthRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.mainUrl}/auth`, credentials)
      .pipe(
        catchError((error) => {
          return throwError(
            () =>
              new Error(error?.error?.message || 'An unknown error occurred'),
          );
        }),
      );
  }

  onLoginToken(
    code: string,
    redirectUri: string,
    organizationApplicationId: number,
  ): Observable<AuthTokenResponse> {
    return this.http
      .post<AuthTokenResponse>(
        `${this.mainUrl}/auth/token`,
        { code, redirectUri, organizationApplicationId },
        { withCredentials: true },
      )
      .pipe(
        tap((response) => {
          sessionStorage.setItem('accessToken', response.accessToken);
        }),
        catchError((error) => {
          return throwError(
            () =>
              new Error(error?.error?.message || 'An unknown error occurred'),
          );
        }),
      );
  }

  onChangePassword(
    data: ChangePassword,
    tokenParam?: string,
  ): Observable<void> {
    const token = tokenParam ? tokenParam : this.getRedirectToken();
    return this.http
      .put<void>(`${this.mainUrl}/auth/change-password`, data, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })
      .pipe(
        catchError((error) => {
          return throwError(
            () =>
              new Error(error?.error?.message || 'An unknown error occurred'),
          );
        }),
      );
  }

  onResetPassword(data: ResetPassword): Observable<void> {
    return this.http
      .post<void>(`${this.mainUrl}/auth/reset-password`, data)
      .pipe(
        catchError((error) => {
          return throwError(
            () =>
              new Error(error?.error?.message || 'An unknown error occurred'),
          );
        }),
      );
  }

  onForgotPassword(data: ForgotPassword): Observable<void> {
    return this.http
      .post<void>(`${this.mainUrl}/auth/forgot-password`, data)
      .pipe(
        catchError((error) => {
          return throwError(
            () =>
              new Error(error?.error?.message || 'An unknown error occurred'),
          );
        }),
      );
  }

  onConfirmEmail(token: string): Observable<void> {
    return this.http
      .post<void>(`${this.mainUrl}/auth/confirm-email`, { token })
      .pipe(
        catchError((error) => {
          return throwError(
            () =>
              new Error(
                error?.error?.message ||
                  error?.title ||
                  'An unknown error occurred',
              ),
          );
        }),
      );
  }

  onRegister(data: RegisterUser): Observable<RegisterUserResponse> {
    return this.http
      .post<RegisterUserResponse>(`${this.mainUrl}/public/register`, data)
      .pipe(
        catchError((error) => {
          return throwError(
            () =>
              new Error(
                error?.error?.message ||
                  error?.title ||
                  'An unknown error occurred',
              ),
          );
        }),
      );
  }

  onRegisterPersonUser(
    data: PersonUserCreate,
  ): Observable<RegisterUserResponse> {
    return this.http
      .post<RegisterUserResponse>(
        `${this.mainUrl}/public/register-person-user`,
        data,
      )
      .pipe(
        catchError((error) => {
          return throwError(
            () =>
              new Error(
                error?.error?.message ||
                  error?.title ||
                  'An unknown error occurred',
              ),
          );
        }),
      );
  }

  /**
   * Deslogea el usuario del sistema.
   */
  onLogout() {
    const applicationId = this.tokenDecoder.getApplicationId();
    this.http.post(`${this.mainUrl}/auth/logout`, {}).subscribe({
      next: () => {
        sessionStorage.clear();
        window.location.href = `${this.gobIdLogin}/app/${applicationId}/login`;
      },
      error: () => {
        sessionStorage.clear();
        window.location.href = `${this.gobIdLogin}/app/${applicationId}/login`;
      },
    });
  }

  /**
   * Refresca el token del usuario usando el refresh token enviado automáticamente via HTTP-only cookie.
   * En caso de que el refreshToken expire, el usuario será deslogeado.
   * @returns
   */
  onRefreshToken() {
    return this.http
      .post<RefreshTokenResponse>(
        `${this.mainUrl}/auth/refresh`,
        {},
        { withCredentials: true },
      )
      .pipe(
        tap((response) => {
          sessionStorage.setItem('accessToken', response.accessToken);
        }),
        catchError((error: ErrorResponse) => {
          this.onLogout();
          return throwError(
            () => new Error(error?.error?.title || 'An unknown error occurred'),
          );
        }),
      );
  }

  /**
   * Consigue el accessToken del usuario.
   * @returns Retorna el accessToken.
   */
  getAccessToken() {
    return sessionStorage.getItem('accessToken') || '';
  }

  getRedirectToken() {
    return sessionStorage.getItem('redirectToken') || '';
  }
}
