import { Routes } from '@angular/router';
import { AuthLayout } from '../../components/ui/layouts/auth/auth.layout';
import { Auth } from './auth';
import { loginGuard } from '../../guards';
import { ChangePassword } from './change-password/change-password';
import { ForgotPassword } from './forgot-password/forgot-password';
import { Register } from './register/register';
import { publicAppGuard } from '../../guards/public-app.guard';

export const AuthRoutes: Routes = [
  {
    component: AuthLayout,
    path: 'app',
    children: [
      {
        path: ':application-id/login',
        component: Auth,
        canActivate: [loginGuard],
      },
      {
        path: ':application-id/forgot-password',
        component: ForgotPassword,
        canActivate: [loginGuard],
      },
      {
        path: ':application-id/change-password',
        component: ChangePassword,
        canActivate: [loginGuard],
      },
      {
        path: ':application-id/register',
        component: Register,
        canActivate: [loginGuard, publicAppGuard],
      },
    ],
  },
];
