import { Component, inject, signal, OnInit } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApplicationAdapted } from '../../../models';
import {
  ApplicationService,
  AuthService,
  NotificationService,
} from '../../../services';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  passwordMatchValidator,
  passwordStrengthValidator,
} from '../../../validators';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-change-password',
  imports: [ReactiveFormsModule, TranslocoModule],
  templateUrl: './change-password.html',
})
export class ChangePassword implements OnInit {
  protected application = signal<ApplicationAdapted>({} as ApplicationAdapted);
  protected isLoading = signal(false);
  protected isPending = signal(false);

  protected submitted = false;
  protected showPassword = signal(false);
  protected showConfirmPassword = signal(false);

  private authService = inject(AuthService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private appService = inject(ApplicationService);
  private transloco = inject(TranslocoService);

  private applicationId =
    this.route.snapshot.paramMap.get('application-id') || '';
  private tokenParams = this.route.snapshot.queryParamMap.get('token') || '';

  private fb = new FormBuilder();

  form = this.fb.group({
    password: ['', [Validators.required, passwordStrengthValidator]],
    confirmPassword: ['', [Validators.required, passwordMatchValidator]],
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
    } catch (_error) {
      this.router.navigate(['/application-404']);
    } finally {
      this.isLoading.set(false);
    }
  };

  missingFields() {
    this.notification.error(this.transloco.translate('auth.toasts.missingFields'));
  }

  async onSubmit() {
    try {
      this.isPending.set(true);
      const passwordData = {
        newPassword: this.form.get('password')?.value || '',
        confirmNewPassword: this.form.get('confirmPassword')?.value || '',
      };
      if (!this.form.valid) {
        this.submitted = true;
        if (passwordData.newPassword !== passwordData.confirmNewPassword) {
          this.notification.onToast({
            message: this.transloco.translate('common.passwordsMismatch'),
            status: 'error',
          });
          return;
        }
        return;
      }

      await firstValueFrom(
        this.authService.onResetPassword({
          newPassword: passwordData.newPassword,
          confirmNewPassword: passwordData.confirmNewPassword,
          token: this.tokenParams,
        }),
      );

      this.notification.onToast({
        message: this.transloco.translate('changePwd.toasts.success'),
        status: 'success',
      });

      if (this.tokenParams) {
        this.router.navigate([`/app/${this.applicationId}/login`]);
        return;
      }

      const redirectUrl = sessionStorage.getItem('redirectUrl');
      window.location.href = redirectUrl || '/';
    } catch (_error) {
      this.submitted = true;
      this.notification.onToast({
        message: this.transloco.translate('changePwd.toasts.error'),
        status: 'error',
      });
    } finally {
      this.isPending.set(false);
    }
  }
}
