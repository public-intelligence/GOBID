import {
  Component,
  computed,
  inject,
  Input,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { injectActiveLang } from '../../../core/i18n/active-lang';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import { DetailsPanel } from '../../../components/ui/details-panel/details-panel';
import { InputComponent, SelectComponent } from '../../../components/shared';
import { PersonAdapted, PersonUserCreate } from '../../../models';
import { Router } from '@angular/router';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import {
  NotificationService,
  PersonService,
  UtilsService,
} from '../../../services';
import { InfoModalComponent } from '../../../components/ui/modals/info-modal/info-modal';

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
  selector: 'app-register-self-person',
  imports: [
    ReactiveFormsModule,
    DetailsPanel,
    InputComponent,
    SelectComponent,
    InfoModalComponent,
    TranslocoModule,
  ],
  templateUrl: './register-self-person.html',
})
export class RegisterSelfPerson implements OnInit {
  @ViewChild(InfoModalComponent)
  confirmationModal!: InfoModalComponent;

  @Input() onCreate!: (personData: PersonUserCreate) => Promise<void>;
  @Input() identificationType = '';
  @Input() identificationNumber = '';

  protected isLoading = signal(false);
  protected personData = signal<PersonAdapted | null>(null);
  protected mask = signal<string | null>(null);
  protected identificationTypeId = signal<string | null>(null);

  private router = inject(Router);
  // private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private personService = inject(PersonService);
  private utils = inject(UtilsService);
  private transloco = inject(TranslocoService);
  private i18n = injectActiveLang();

  // private personId = this.activatedRoute.snapshot.params['id'];

  fb = new FormBuilder();

  // Opciones traducidas; se recalculan al cambiar de idioma (activeLang()). Los `value`
  // se mantienen (códigos que el backend espera); solo cambia la etiqueta visible.
  genders = computed(() => {
    this.i18n.activeLang();
    return [
      { label: this.transloco.translate('register.gender.male'), value: 'Male' },
      { label: this.transloco.translate('register.gender.female'), value: 'Female' },
      { label: this.transloco.translate('register.gender.other'), value: 'Other' },
    ];
  });

  isExternalOptions = computed(() => {
    this.i18n.activeLang();
    return [
      { label: this.transloco.translate('common.yes'), value: 'true' },
      { label: this.transloco.translate('common.no'), value: 'false' },
    ];
  });

  identificationTypes = computed(() => {
    this.i18n.activeLang();
    return [
      { label: this.transloco.translate('common.cedula'), value: '1' },
      { label: this.transloco.translate('common.passport'), value: '2' },
    ];
  });

  form = this.fb.group(
    {
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      identificationNumber: ['', Validators.required],
      gender: ['Male', Validators.required],
      email: ['', [Validators.email, Validators.required]],
      phoneNumber: [''],
      birthdate: [''],
      isExternal: ['true', Validators.required],
      identificationType: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(6)]],
    },
    { validators: passwordsMatchValidator },
  );

  async ngOnInit() {
    this.form.controls['identificationNumber'].valueChanges.subscribe(
      (value) => {
        if (!value) return;
        const upper = value
          .split('')
          .map((c) => this.utils.toUpperIfLetter(c))
          .join('');
        if (upper !== value) {
          this.form.controls['identificationNumber'].setValue(upper, {
            emitEvent: false,
          });
        }
      },
    );

    if (this.identificationNumber) {
      this.form.controls['identificationNumber'].setValue(
        this.identificationNumber,
      );
      this.form.controls['identificationNumber'].disable();
      this.form.controls['identificationType'].setValue(
        this.identificationType,
      );
      this.form.controls['identificationType'].disable();
    }
    this.isLoading.set(true);
    this.isLoading.set(false);
  }

  get formData(): PersonUserCreate {
    return {
      firstName: this.form.get('firstName')?.value?.toUpperCase() || '',
      lastName: this.form.get('lastName')?.value?.toUpperCase() || '',
      identificationNumber:
        this.form.get('identificationNumber')?.value?.replace(/-/g, '') || '',
      gender: this.form.get('gender')?.value || '',
      email: this.form.get('email')?.value || '',
      phoneNumber:
        this.form.get('phoneNumber')?.value?.replace(/\D/g, '') || '',
      birthdate: this.form.get('birthdate')?.value || '',
      isExternal: this.form.get('isExternal')?.value === 'true',
      status: this.personData()?.status ?? 'Active',
      password: this.form.get('password')?.value || '',
      confirmPassword: this.form.get('confirmPassword')?.value || '',
    };
  }

  getIdentificationTypeId = (event: Event) => {
    const target = event.target as HTMLSelectElement;
    this.identificationTypeId.set(target.value || null);
  };

  isValidForm() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notification.error(
        this.transloco.translate('register.toasts.completeFieldsCorrectly'),
      );
      return false;
    }
    return true;
  }

  async onSubmit() {
    try {
      if (!this.isValidForm()) return;
      this.isLoading.set(true);
      await this.onCreate(this.formData);
    } catch (error) {
      this.notification.error(
        (error as Error).message ||
          this.transloco.translate('register.toasts.formError'),
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
