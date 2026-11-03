import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services';
import { inject } from '@angular/core';
import { LanguageService } from '../core/i18n/language.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const accessToken = authService.getAccessToken();
  // Idioma activo → el backend puede devolver contenido dinámico localizado.
  const lang = inject(LanguageService).getActiveLang();

  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`,
      'Accept-Language': lang,
    },
  });

  if (req.url.includes('/auth')) {
    return next(req.clone({ setHeaders: { 'Accept-Language': lang } }));
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        return authService.onRefreshToken().pipe(
          switchMap(() => {
            const newAccessToken = authService.getAccessToken();
            const updateHeaders = req.headers.set(
              'Authorization',
              `Bearer ${newAccessToken}`,
            );
            const newAuthReq = req.clone({ headers: updateHeaders });
            return next(newAuthReq);
          }),
        );
      }
      if (error.status === 403 && error.error?.Message.includes('política')) {
        authService.onLogout();
        sessionStorage.clear();
      }
      return throwError(() => error);
    }),
  );
};
