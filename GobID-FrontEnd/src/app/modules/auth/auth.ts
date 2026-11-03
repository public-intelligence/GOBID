import { Component, inject, OnInit, signal } from '@angular/core';
import { ApplicationAdapted, UserOrgApp } from '../../models';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationService,
  AuthService,
  NotificationService,
} from '../../services';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { environment } from '../../../environments/environment';
import { Title } from '@angular/platform-browser';
import { NgClass, TitleCasePipe } from '@angular/common';
import { CredentialsLoaderComponent } from '@public-intelligence/ui-core';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-auth',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    NgClass,
    TitleCasePipe,
    CredentialsLoaderComponent,
    TranslocoModule,
  ],
  templateUrl: './auth.html',
})
export class Auth implements OnInit {
  protected application = signal<ApplicationAdapted>({} as ApplicationAdapted);
  protected isLoading = signal(false);
  protected isPending = signal(false);
  protected isRedirecting = signal(false);
  protected userOrgApps = signal<UserOrgApp[]>([]);
  protected showOrgSelector = signal(false);
  protected appSelected = signal<boolean>(false);

  protected submitted = false;
  protected isPublic = signal(false);
  protected showPassword = signal(false);

  private pendingCode = '';
  private pendingMustChangePassword = false;

  private authService = inject(AuthService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private appService = inject(ApplicationService);
  private Title = inject(Title);
  private transloco = inject(TranslocoService);

  private applicationId =
    this.route.snapshot.paramMap.get('application-id') || '';

  protected forgotPasswordPath = `/app/${this.applicationId}/forgot-password`;
  protected registerPath = `/app/${this.applicationId}/register`;

  private fb = new FormBuilder();

  loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    isTrustedDevice: [false],
  });

  ngOnInit() {
    this.getApplication();
  }

  getApplication = async () => {
    try {
      this.isLoading.set(true);
      const application = await firstValueFrom(
        this.appService.findPublic(this.applicationId),
      );
      this.application.set(application);
      this.Title.setTitle(application.name);
      this.isPublic.set(application.isPublic);
    } catch (_error) {
      this.router.navigate(['/application-404']);
    } finally {
      this.isLoading.set(false);
    }
  };

  getPathnameWithoutParams(): string {
    return window.location.pathname;
  }

  missingFields() {
    this.notification.error(this.transloco.translate('auth.toasts.missingFields'));
  }

  async onSubmit() {
    if (!this.loginForm.valid) {
      this.missingFields();
      return;
    }
    try {
      this.isPending.set(true);
      const loginData = {
        email: this.loginForm.get('email')?.value || '',
        password: this.loginForm.get('password')?.value || '',
        isTrustedDevice: this.loginForm.get('isTrustedDevice')?.value ?? false,
      };
      const response = await firstValueFrom(
        this.authService.onLogin({
          login: loginData.email,
          password: loginData.password,
          applicationId: this.applicationId,
          isTrustedDevice: loginData.isTrustedDevice,
        }),
      );

      this.pendingCode = response.code;
      this.pendingMustChangePassword = response.mustChangePassword;

      if (this.pendingMustChangePassword) {
        this.router.navigate([`/app/${this.applicationId}/change-password`], {
          queryParams: { token: response.passwordResetToken },
        });
        return;
      }

      if (response.userOrgApps.length > 1) {
        this.userOrgApps.set(response.userOrgApps);
        this.showOrgSelector.set(true);
        return;
      }

      await this.completeLogin(response.userOrgApps[0]);
    } catch (error) {
      this.submitted = true;
      this.notification.onToast({
        message:
          (error as Error)?.message ||
          this.transloco.translate('auth.toasts.invalidCredentials'),
        status: 'error',
      });
    } finally {
      this.isPending.set(false);
    }
  }

  async selectOrgApp(orgApp: UserOrgApp) {
    try {
      this.isPending.set(true);
      this.appSelected.set(true);
      await this.completeLogin(orgApp);
    } catch (error) {
      this.notification.onToast({
        message:
          (error as Error)?.message ||
          this.transloco.translate('auth.toasts.loginError'),
        status: 'error',
      });
    } finally {
      this.isPending.set(false);
      this.appSelected.set(false);
    }
  }

  private async completeLogin(orgApp: UserOrgApp) {
    const code = this.pendingCode;
    const appUrl = environment.appUrl;
    const localAppId = environment.applicationId;
    const redirectUrl = this.application().redirectUrl;
    const appicationId = String(this.application().id);
    const isSameApp = localAppId?.toLowerCase() === appicationId?.toLowerCase();

    if (!orgApp.policyAccess) {
      this.notification.onToast({
        message:
          orgApp.policyMessage === ''
            ? this.transloco.translate('auth.policyDenied')
            : orgApp.policyMessage,
        status: 'error',
      });
      return;
    }

    this.isRedirecting.set(true);

    if (!isSameApp) {
      window.location.href = `${redirectUrl}?code=${code}&orgAppId=${orgApp.organizationApplicationId}`;
      return;
    }

    await firstValueFrom(
      this.authService.onLoginToken(
        code,
        appUrl,
        orgApp.organizationApplicationId,
      ),
    );

    if (this.pendingMustChangePassword) {
      const path = isSameApp
        ? `/dashboard`
        : `${redirectUrl}?code=${code}&orgAppId=${orgApp.organizationApplicationId}`;
      sessionStorage.setItem('mustChangePassword', 'true');
      sessionStorage.setItem('redirectUrl', path);
      this.router.navigate([`/app/${this.applicationId}/change-password`]);
      return;
    }

    this.router.navigate(['/dashboard']);
  }
}
