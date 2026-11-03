import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApplicationAdapted } from '../../../models';
import {
  ApplicationService,
  AuthService,
  NotificationService,
} from '../../../services';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InfoModalComponent } from '../../../components/ui/modals/info-modal/info-modal';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, InfoModalComponent, TranslocoModule],
  templateUrl: './forgot-password.html',
})
export class ForgotPassword implements OnInit {
  @ViewChild(InfoModalComponent)
  confirmationModal!: InfoModalComponent;
  protected application = signal<ApplicationAdapted>({} as ApplicationAdapted);
  protected isLoading = signal(false);
  protected isPending = signal(false);

  protected submitted = false;

  private authService = inject(AuthService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private appService = inject(ApplicationService);
  private transloco = inject(TranslocoService);

  private applicationId =
    this.route.snapshot.paramMap.get('application-id') || '';

  faArrowLeft = null;

  private fb = new FormBuilder();

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
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

  navigateToLogin() {
    this.router.navigate([`/app/${this.applicationId}/login`]);
  }

  missingFields() {
    this.notification.error(this.transloco.translate('auth.toasts.missingFields'));
  }

  async onSubmit() {
    try {
      this.isPending.set(true);
      const passwordData = {
        email: this.form.get('email')?.value || '',
      };
      if (!this.form.valid) {
        this.submitted = true;
        this.notification.onToast({
          message: this.transloco.translate('forgot.toasts.emailInvalid'),
          status: 'error',
        });
        return;
      }

      await firstValueFrom(
        this.authService.onForgotPassword({
          email: passwordData.email,
          applicationId: this.applicationId,
        }),
      );

      this.showModal(passwordData.email);
      /* this.notification.onToast({
        message:
          'Se ha enviado un correo electrónico para restablecer la contraseña',
        status: 'default',
      }); */
    } catch (_error) {
      this.submitted = true;
      this.notification.onToast({
        message: this.transloco.translate('forgot.toasts.resetError'),
        status: 'error',
      });
    } finally {
      this.isPending.set(false);
    }
  }

  showModal(email: string) {
    this.confirmationModal.openModal({
      title: this.transloco.translate('forgot.modal.title'),
      name: email,
      paragraph: this.transloco.translate('forgot.modal.body'),
      confirmButton: this.transloco.translate('common.close'),
      onConfirm: () => {
        this.router.navigate([`/app/${this.applicationId}/login`]);
      },
    });
  }
}
