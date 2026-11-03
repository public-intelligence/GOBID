import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { LayoutService } from '@public-intelligence/ui-core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputComponent } from '../../../../components/shared';
import {
  LegalEntityAdapted,
  LegalEntityCreate,
  Select,
} from '../../../../models';
import { DetailsPanel } from '../../../../components/ui/details-panel/details-panel';
import { ActivatedRoute, Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { LegalEntityService, NotificationService } from '../../../../services';
import { firstValueFrom } from 'rxjs';
import { ConfirmationModalComponent } from '../../../../components/ui/modals/confirmation-modal/confirmation-modal';

@Component({
  selector: 'app-organization-maintenance',
  imports: [
    ReactiveFormsModule,
    InputComponent,
    DetailsPanel,
    FontAwesomeModule,
    ConfirmationModalComponent,
  ],
  templateUrl: './legal-entity-maintenance.html',
})
export class LegalEntityMaintenance implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  container = 'p-6 bg-white border border-gray-100 rounded-lg shadow-sm';

  private layoutService = inject(LayoutService);

  protected isLoading = signal(false);
  protected organizationData = signal<LegalEntityAdapted | null>(null);

  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private legalEntityService = inject(LegalEntityService);

  private orgId = this.activatedRoute.snapshot.params['id'];
  protected isEdit = Boolean(this.orgId);

  fb = new FormBuilder();

  form = this.fb.group({
    legalName: ['', Validators.required],
    socialName: ['', Validators.required],
    rnc: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    type: ['private', Validators.required],
  });

  organizationTypeOptions: Select[] = [
    {
      label: 'Privada',
      value: 'Privada',
    },
    {
      label: 'Pública',
      value: 'Pública',
    },
  ];

  async ngOnInit() {
    this.layoutService.setPageTitle('Mantenimiento de Organización');
    this.layoutService.breadcrumbs.set([
      { label: 'Entidades Legales', url: '/identity-hub/legal-entity' },
      { label: 'Mantenimiento', url: '/identity-hub/organization' },
    ]);
    if (!this.orgId) return;
    this.isLoading.set(true);
    await this.getOrganizationById();
    this.isLoading.set(false);
  }

  get formData(): LegalEntityCreate {
    return {
      id: this.isEdit ? this.orgId : undefined,
      legalName: this.form.get('legalName')?.value || '',
      socialName: this.form.get('socialName')?.value || '',
      rnc: this.form.get('rnc')?.value || '',
      email: this.form.get('email')?.value || '',
      phoneNumber: this.form.get('phoneNumber')?.value || '',
      type: this.form.get('type')?.value || '',
      status: '1',
    };
  }

  async getOrganizationById() {
    try {
      const response = await firstValueFrom(
        this.legalEntityService.findOne(this.orgId),
      );
      this.organizationData.set(response);
      this.form.patchValue({
        legalName: response.legalName,
        socialName: response.socialName,
        rnc: response.identificationNumber,
        email: response.email,
        phoneNumber: response.phoneNumber,
        type: response.type,
      });
    } catch (_error) {
      this.notification.error('Error al obtener la organización.');
    }
  }

  goBackToList() {
    this.router.navigate(['/identity-hub/legal-entity']);
  }

  isValidForm() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notification.error('Por favor, complete todos los campos.');
      return false;
    }
    return true;
  }

  async onCreate() {
    try {
      await firstValueFrom(this.legalEntityService.create(this.formData));
      this.notification.success('Organización registrada correctamente.');
      this.router.navigate(['/identity-hub/organization']);
    } catch (error) {
      this.notification.error(
        (error as Error).message || 'Error al registrar la organización.',
      );
    }
  }

  async onUpdate() {
    try {
      await firstValueFrom(
        this.legalEntityService.update(this.orgId, this.formData),
      );
      this.notification.success('Organización actualizada correctamente.');
      this.router.navigate(['/identity-hub/organization']);
    } catch (error) {
      this.notification.error(
        (error as Error).message || 'Error al actualizar la organización.',
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
