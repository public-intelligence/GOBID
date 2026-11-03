import { isPlatformServer } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { environment } from '../../environments/environment';

export const authGuard: CanActivateFn = (route: ActivatedRouteSnapshot) => {
  const platformId = inject(PLATFORM_ID);
  const router = inject(Router);
  const loginPth = `/app/${environment.applicationId}/login`;

  if (isPlatformServer(platformId)) {
    return false;
  }

  const tokenUrl = route.queryParamMap.get('accessToken');

  if (tokenUrl) {
    sessionStorage.setItem('accessToken', tokenUrl);
    router.navigateByUrl('/dashboard', { replaceUrl: true });
  }

  const token = sessionStorage.getItem('accessToken');

  if (token) {
    return true;
  } else {
    router.navigate([loginPth]);
    return false;
  }
};
