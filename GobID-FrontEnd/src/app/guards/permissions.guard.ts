import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UtilsService } from '../services';

export const permissionsGuard: CanActivateFn = (route, _state) => {
  const router = inject(Router);
  const utilsService = inject(UtilsService);
  const permissions = route.data['permissions'] as string[];

  const hasPermission = utilsService.hasPermission(permissions);

  if (!hasPermission) {
    router.navigateByUrl('/access-denied', { replaceUrl: true });
    return false;
  }
  return true;
};
