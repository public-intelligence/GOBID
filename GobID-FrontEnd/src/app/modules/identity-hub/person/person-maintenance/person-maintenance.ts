import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { LayoutService } from '@public-intelligence/ui-core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputComponent, SelectComponent } from '../../../../components/shared';
import { PersonAdapted, PersonCreate } from '../../../../models';
import { DetailsPanel } from '../../../../components/ui/details-panel/details-panel';
import { ActivatedRoute, Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  PersonService,
  NotificationService,
  UtilsService,
} from '../../../../services';
import { firstValueFrom } from 'rxjs';
import { ConfirmationModalComponent } from '../../../../components/ui/modals/confirmation-modal/confirmation-modal';

@Component({
  selector: 'app-person-maintenance',
  imports: [
    ReactiveFormsModule,
    InputComponent,
    SelectComponent,
    DetailsPanel,
    FontAwesomeModule,
    ConfirmationModalComponent,
  ],
  templateUrl: './person-maintenance.html',
})
export class PersonMaintenance implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  private layoutService = inject(LayoutService);

  protected isLoading = signal(false);
  protected personData = signal<PersonAdapted | null>(null);
  protected mask = signal<string | null>(null);
  protected identificationTypeId = signal<string | null>(null);

  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private personService = inject(PersonService);
  private utils = inject(UtilsService);

  private personId = this.activatedRoute.snapshot.params['id'];
  protected isEdit = Boolean(this.personId);

  fb = new FormBuilder();

  genders = [
    { label: 'Masculino', value: 'Male' },
    { label: 'Femenino', value: 'Female' },
    { label: 'Otro', value: 'Other' },
  ];

  isExternalOptions = [
    { label: 'Sí', value: 'true' },
    { label: 'No', value: 'false' },
  ];

  identificationTypes = [
    { label: 'Cédula de Identidad y electoral', value: '1' },
    { label: 'Pasaporte', value: '2' },
  ];

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    identificationNumber: ['', Validators.required],
    gender: ['Male', Validators.required],
    email: ['', [Validators.email]],
    phoneNumber: [''],
    birthdate: [''],
    isExternal: ['true', Validators.required],
  });

  async ngOnInit() {
    this.layoutService.setPageTitle('Mantenimiento de Personas');
    this.layoutService.breadcrumbs.set([
      { label: 'Personas', url: '/identity-hub/person' },
      { label: 'Mantenimiento', url: '/identity-hub/person/maintenance' },
    ]);
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

    if (!this.personId) return;
    this.isLoading.set(true);
    await this.getPersonById();
    this.isLoading.set(false);
  }

  get formData(): PersonCreate {
    return {
      id: this.isEdit ? this.personId : undefined,
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
    };
  }

  async getPersonById() {
    try {
      const response = await firstValueFrom(
        this.personService.findOne(this.personId),
      );
      this.personData.set(response);
      this.form.patchValue({
        firstName: response.firstName,
        lastName: response.lastName,
        identificationNumber: response.identificationNumber,
        gender: response.gender,
        email: response.email,
        phoneNumber: response.phoneNumber,
        isExternal: response.isExternal ? 'true' : 'false',
      });
    } catch (_error) {
      this.notification.error('Error al obtener el personal.');
    }
  }

  goBackToList() {
    this.router.navigate(['/identity-hub/person']);
  }

  getIdentificationTypeId = (event: Event) => {
    const target = event.target as HTMLSelectElement;
    this.identificationTypeId.set(target.value || null);
  };

  isValidForm() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notification.error(
        'Favor de completar todos los campos requeridos correctamente.',
      );
      return false;
    }
    return true;
  }

  async onCreate() {
    try {
      await firstValueFrom(this.personService.create(this.formData));
      this.notification.success('Personal registrado correctamente.');
      this.router.navigate(['/identity-hub/person']);
    } catch (error) {
      this.notification.error(
        (error as Error).message || 'Error al registrar el personal.',
      );
    }
  }

  async onUpdate() {
    try {
      await firstValueFrom(
        this.personService.update(this.personId, this.formData),
      );
      this.notification.success('Personal actualizado correctamente.');
      this.router.navigate(['/identity-hub/person']);
    } catch (error) {
      this.notification.error(
        (error as Error).message || 'Error al actualizar el personal.',
      );
    }
  }

  async onSubmit() {
    try {
      if (!this.isValidForm()) return;
      this.isLoading.set(true);
      if (this.isEdit) {
        await this.onUpdate();
      } else {
        await this.onCreate();
      }
    } catch (error) {
      this.notification.error(
        (error as Error).message ||
          'Ocurrió un error al procesar el formulario.',
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
