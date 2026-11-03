import { Routes } from '@angular/router';
import { AuthRoutes } from './modules/auth/auth.routes';
import { MainLayout } from './components/ui/layouts/main/main.layout';
import { Dashboard } from './modules/dashboard/dashboard';
import { ApplicationRoutes } from './modules/application/application.routes';
import { authGuard } from './guards/auth.guard';
import { ApplicationNotFound } from './modules/application-not-found/application-not-found';
import { OrganizationRoutes } from './modules/organization/organization.routes';
import { UserRoutes } from './modules/users/user.routes';
import { OrganizationApplicationRoutes } from './modules/organization-application/organization-application.routes';
import { UserApplicationRoutes } from './modules/user-app/user-app.routes';
import { NotFound } from './modules/not-found/not-found';
import { AccessDenied } from './modules/access-denied/access-denied';
import { RoleApplicationRoutes } from './modules/role-app/role-app.routes';
import { IdentityHubRoutes } from './modules/identity-hub/identity.routes';
import { VerifyEmail } from './modules/auth/verify-email/verify-email';
import { SessionRoutes } from './modules/sessions/sessions.routes';
import { PolicyRoutes } from './modules/policy/policy.routes';
import { PersonnelRoutes } from './modules/personnel/personnel.routes';

export const routes: Routes = [
  ...AuthRoutes,
  {
    component: MainLayout,
    path: '',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        component: Dashboard,
      },
      ...ApplicationRoutes,
      ...OrganizationRoutes,
      ...UserRoutes,
      ...OrganizationApplicationRoutes,
      ...UserApplicationRoutes,
      ...RoleApplicationRoutes,
      ...IdentityHubRoutes,
      ...SessionRoutes,
      ...PolicyRoutes,
      ...PersonnelRoutes,
    ],
  },
  {
    path: 'verify/email',
    component: VerifyEmail,
  },
  {
    path: 'application-404',
    component: ApplicationNotFound,
  },
  {
    path: 'access-denied',
    component: AccessDenied,
  },
  {
    path: '**',
    redirectTo: '404',
  },
  {
    path: '404',
    component: NotFound,
  },
];
