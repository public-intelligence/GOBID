import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { ApplicationService } from '../services';
import { firstValueFrom } from 'rxjs';

export const publicAppGuard: CanActivateFn = async (
  route: ActivatedRouteSnapshot,
) => {
  const appService = inject(ApplicationService);
  const router = inject(Router);

  const id = route.paramMap.get('application-id');
  const loginPth = `/app/${id}/login`;

  if (!id) {
    return router.createUrlTree(['/not-found']);
  }

  try {
    const app = await firstValueFrom(appService.findPublic(id));

    return app.isPublic ? true : router.createUrlTree([loginPth]);
  } catch (_error) {
    // fallback si falla la API
    return router.createUrlTree([loginPth]);
  }
};
