import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationAdapted,
  PersonAdapted,
  PersonCreate,
  PersonUserCreate,
  RegisterUser,
  Select,
} from '../../../models';
import {
  ApplicationService,
  AuthService,
  NotificationService,
  PersonService,
} from '../../../services';
import { ActivatedRoute, Router } from '@angular/router';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { SearchComponent } from '../../../components/shared';
import { passwordStrengthValidator } from '../../../validators';
import { InfoModalComponent } from '../../../components/ui/modals/info-modal/info-modal';
import { RegisterSelfPerson } from '../register-self-person/register-self-person';
import { NgClass } from '@angular/common';
import { NgxMaskDirective, provideNgxMask } from 'ngx-mask';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { injectActiveLang } from '../../../core/i18n/active-lang';

const passwordsMatchValidator: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');
  if (password && confirmPassword && password.value !== confirmPassword.value) {
    return { passwordsMismatch: true };
  }
  return null;
};

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    SearchComponent,
    InfoModalComponent,
    RegisterSelfPerson,
    NgClass,
    NgxMaskDirective,
    TranslocoModule,
  ],
  templateUrl: './register.html',
  providers: [provideNgxMask()],
})
export class Register implements OnInit {
  @ViewChild(SearchComponent) searchComponent?: SearchComponent;
  @ViewChild(InfoModalComponent)
  confirmationModal!: InfoModalComponent;

  protected application = signal<ApplicationAdapted>({} as ApplicationAdapted);
  protected isLoading = signal(false);
  protected isPending = signal(false);
  protected isSearching = signal(false);
  protected identificationTypeId = signal<string | null>(null);
  protected person = signal<PersonAdapted | null>(null);
  protected showPassword = signal(false);
  protected showConfirmPassword = signal(false);
  protected submitted = false;
  private transloco = inject(TranslocoService);
  private i18n = injectActiveLang();
  // Opciones traducidas; se recalculan al cambiar de idioma (activeLang()).
  protected identificationType = computed<Select[]>(() => {
    this.i18n.activeLang();
    return [
      { label: this.transloco.translate('common.cedula'), value: '1' },
      { label: this.transloco.translate('common.passport'), value: '2' },
    ];
  });
  protected identificationNumber = signal<string>('');
  protected steps = signal<number>(1);

  private authService = inject(AuthService);
  private personService = inject(PersonService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private appService = inject(ApplicationService);

  private applicationId =
    this.route.snapshot.paramMap.get('application-id') || '';

  protected loginPath = `/app/${this.applicationId}/login`;

  private fb = new FormBuilder();

  registerForm = this.fb.group(
    {
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(6),
          passwordStrengthValidator,
        ],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator },
  );

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

  async searchPerson(documentNumber: string) {
    if (!documentNumber || !this.identificationTypeId()) return;

    const isPassaport = this.identificationTypeId() === '2';
    this.identificationNumber.set(documentNumber);

    try {
      this.isSearching.set(true);
      const person = await firstValueFrom(
        this.personService.findByIdentificationNumberPublic(
          documentNumber,
          isPassaport,
        ),
      );
      this.person.set(person);
      this.steps.set(3);
    } catch (_error) {
      if (this.identificationTypeId() === '1') {
        this.notification.onToast({
          message: this.transloco.translate('register.toasts.personNotFound'),
          status: 'error',
        });
        return;
      }
      this.steps.set(2);
    } finally {
      this.isSearching.set(false);
      this.searchComponent?.clearSearch();
    }
  }

  clearPerson() {
    this.person.set(null);
    this.registerForm.reset();
    this.submitted = false;
    this.steps.set(1);
    this.identificationTypeId.set('');
  }

  async onRegisterUser() {
    this.submitted = true;
    if (!this.registerForm.valid || !this.person()) {
      this.notification.onToast({
        message: this.transloco.translate('register.toasts.completeFields'),
        status: 'error',
      });
      return;
    }
    try {
      this.isPending.set(true);
      const data: RegisterUser = {
        email: this.registerForm.get('email')?.value || '',
        phoneNumber: this.registerForm.get('phoneNumber')?.value || '',
        password: this.registerForm.get('password')?.value || '',
        confirmPassword: this.registerForm.get('confirmPassword')?.value || '',
        personnelId: this.person()!.id,
      };
      const response = await firstValueFrom(this.authService.onRegister(data));

      this.showModal(response.message);
    } catch (error: any) {
      this.notification.onToast({
        message:
          error?.message ||
          this.transloco.translate('register.toasts.registerError'),
        status: 'error',
      });
    } finally {
      this.isPending.set(false);
    }
  }

  onRegisterUserPerson = async (personData: PersonUserCreate) => {
    try {
      const response = await firstValueFrom(
        this.authService.onRegisterPersonUser(personData),
      );
      this.showModal(response.message);
    } catch (error) {
      this.notification.error(
        (error as Error).message ||
          this.transloco.translate('register.toasts.registerError'),
      );
    }
  };

  showModal(message: string) {
    this.confirmationModal.openModal({
      title: this.transloco.translate('register.modal.title'),
      name: '',
      paragraph: message || this.transloco.translate('register.modal.body'),
      confirmButton: this.transloco.translate('common.close'),
      onConfirm: () => {
        this.navigateToLogin();
      },
    });
  }

  getIdentificationTypeId = (event: Event) => {
    const target = event.target as HTMLSelectElement;
    this.identificationTypeId.set(target.value || null);
  };

  navigateToLogin() {
    this.router.navigate([this.loginPath]);
  }
}
