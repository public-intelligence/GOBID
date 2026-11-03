import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { StepsForm } from '../../../components/ui';
import { permissionHeaders, steps } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { CheckboxComponent, InputComponent } from '../../../components/shared';
import {
  ApplicationAdapted,
  ApplicationCreate,
  ApplicationCreateResponse,
  ClaimAdapted,
  ClaimCreate,
  ColumnData,
} from '../../../models';
import { TextAreaComponent } from '../../../components/shared/textarea/textarea';
import { DetailsPanel } from '../../../components/ui/details-panel/details-panel';
import { TableComponent } from '../../../components/ui/table/table';
import { faPen, faTrash } from '@fortawesome/free-solid-svg-icons';
import { ActivatedRoute, Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  ApplicationService,
  ClaimService,
  NotificationService,
} from '../../../services';
import { firstValueFrom } from 'rxjs';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';

@Component({
  selector: 'app-application-maintenance',
  imports: [
    StepsForm,
    ReactiveFormsModule,
    InputComponent,
    CheckboxComponent,
    TextAreaComponent,
    DetailsPanel,
    TableComponent,
    FontAwesomeModule,
    ConfirmationModalComponent,
  ],
  templateUrl: './application-maintenance.html',
})
export class ApplicationMaintenance implements OnInit {
  @ViewChild(StepsForm) stepsForm!: StepsForm;
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  steps = steps;
  permissionHeaders = permissionHeaders;

  private layoutService = inject(LayoutService);

  faTrash = faTrash;
  faPen = faPen;

  protected isLoading = signal(false);
  protected claimsData = signal<ClaimAdapted[]>([]);
  protected applicationData = signal<ApplicationAdapted | null>(null);
  protected isPending = signal(false);

  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private applicationService = inject(ApplicationService);
  private claimService = inject(ClaimService);

  private applicationId = this.activatedRoute.snapshot.params['id'];
  protected isEdit = Boolean(this.applicationId);

  fb = new FormBuilder();

  form = this.fb.group({
    name: ['', Validators.required],
    code: ['', Validators.required],
    redirectUrl: ['', Validators.required],
    description: [''],
    isPublic: [false],
    permissionCode: [''],
    permissionName: [''],
    permissionDescription: [''],
    permissionIsAutomatic: [false],
    permissions: this.fb.array([]),
  });

  async ngOnInit() {
    this.layoutService.setPageTitle('Mantenimiento de Aplicación');
    this.layoutService.breadcrumbs.set([
      { label: 'Aplicaciones', url: '/applications' },
      { label: 'Mantenimiento', url: '/applications/maintenance' },
    ]);
    if (!this.applicationId) return;
    this.isLoading.set(true);
    await this.getApplicationById();
    await this.getClaimsByApplication();
    this.isLoading.set(false);
  }

  //////////////// Getters //////////////////

  get currentStep() {
    return this.stepsForm?.steps().current ?? 1;
  }

  get firstStep() {
    return this.currentStep === 1;
  }

  get finalStep() {
    return this.currentStep === this.steps.steps.length;
  }

  get permissions() {
    return this.form.get('permissions')?.value;
  }

  get permissionsData() {
    const permissions = this.form.get('permissions') as FormArray;

    const permissionsDataTable: ColumnData[] = permissions.controls.map(
      (control) => {
        return {
          id: control.get('id')?.value,
          name: control.get('name')?.value,
          actionsOptions: [
            {
              name: 'Eliminar',
              color: 'red',
              icon: faTrash,
              function: (id: string) => {
                if (this.isEdit) {
                  this.showDeleteConfirmation(
                    control.get('id')?.value,
                    `${control.get('name')?.value} - ${control.get('code')?.value}`,
                  );
                } else {
                  this.removePermission(id);
                }
              },
            },
          ],
          columns: [
            { value: control.get('code')?.value, dataType: 'string' },
            { value: control.get('name')?.value, dataType: 'string' },
            { value: control.get('description')?.value, dataType: 'string' },
            {
              value: control.get('isAutomatic')?.value ? 'Si' : 'No',
              dataType: 'string',
              justify: 'center',
            },
          ],
        };
      },
    );
    return permissionsDataTable;
  }

  get permissionsDataNoActions() {
    const permissions = this.form.get('permissions') as FormArray;

    const permissionsDataTable: ColumnData[] = permissions.controls.map(
      (control) => {
        return {
          id: control.get('code')?.value,
          name: control.get('name')?.value,
          showActions: false,
          columns: [
            { value: control.get('code')?.value, dataType: 'string' },
            { value: control.get('name')?.value, dataType: 'string' },
            { value: control.get('description')?.value, dataType: 'string' },
            {
              value: control.get('isAutomatic')?.value ? 'Si' : 'No',
              dataType: 'string',
              justify: 'center',
            },
          ],
        };
      },
    );
    return permissionsDataTable;
  }

  get firstStepValidation() {
    return (
      this.form.get('name')?.valid &&
      this.form.get('code')?.valid &&
      this.form.get('redirectUrl')?.valid &&
      this.form.get('description')?.valid
    );
  }

  get formData(): ApplicationCreate {
    return {
      applicationId: this.isEdit ? this.applicationId : null,
      applicationName: this.form.get('name')?.value || '',
      code: this.form.get('code')?.value || '',
      redirectUrl: this.form.get('redirectUrl')?.value || '',
      applicationDescription: this.form.get('description')?.value || '',
      isActive: this.applicationData()?.isActive ?? true,
      isPublic: this.form.get('isPublic')?.value ?? false,
    };
  }

  //////////////// Services //////////////////
  async getApplicationById() {
    try {
      const response = await firstValueFrom(
        this.applicationService.findOne(this.applicationId),
      );
      this.applicationData.set(response);
      this.form.patchValue({
        name: response.name,
        code: response.code,
        redirectUrl: response.redirectUrl,
        description: response.description,
        isPublic: response.isPublic,
      });
    } catch (_error) {
      this.notification.error('Error al obtener la aplicación.');
    }
  }

  async getClaimsByApplication() {
    try {
      const response = await firstValueFrom(
        this.claimService.findAll({ ApplicationId: this.applicationId }),
      );
      response.data.forEach((claim) => {
        (this.form.get('permissions') as FormArray)?.push(
          this.fb.group({
            id: claim.id,
            code: claim.code,
            name: claim.name,
            description: claim.description,
            isAutomatic: claim.isAutomatic ?? false,
          }),
        );
      });
      this.claimsData.set(response.data);
    } catch (_error) {
      this.notification.error('Error al obtener la aplicación.');
    }
  }

  /////////////////////////////////////////////
  nextStep() {
    this.stepsForm.nextStep();
  }

  previousStep() {
    this.stepsForm.previousStep();
  }

  goToStep(step: number) {
    this.stepsForm.goToStep(step);
  }

  addPermission() {
    if (
      !this.form.get('permissionCode')?.value ||
      !this.form.get('permissionName')?.value
    ) {
      return;
    }
    const permissions = this.form.get('permissions') as FormArray;
    permissions.push(
      this.fb.group({
        id: 0,
        code: this.form.get('permissionCode')?.value,
        name: this.form.get('permissionName')?.value,
        description: this.form.get('permissionDescription')?.value,
        isAutomatic: this.form.get('permissionIsAutomatic')?.value ?? false,
      }),
    );
    this.form.patchValue({
      permissionCode: '',
      permissionName: '',
      permissionDescription: '',
      permissionIsAutomatic: false,
    });
  }

  removePermission = (id: string) => {
    const permissions = this.form.get('permissions') as FormArray;
    const index = permissions.controls.findIndex(
      (control) => control.get('id')?.value === id,
    );

    if (index !== -1) {
      permissions.removeAt(index);
    }
  };

  goBackToList() {
    this.router.navigate(['/applications']);
  }

  isValidForm() {
    if (this.form.invalid) {
      this.notification.error(
        'Favor de completar todos los campos requeridos.',
      );
      return false;
    }
    return true;
  }

  formattedPermissions(id?: number): ClaimCreate[] {
    const permissions = this.form.get('permissions') as FormArray;
    return permissions.controls.map((control) => {
      return {
        claimId: control.get('id')?.value ? control.get('id')?.value : 0,
        applicationId: id ?? null,
        code: control.get('code')?.value,
        claimName: control.get('name')?.value,
        claimDescription: control.get('description')?.value,
        isActive: true,
        isAutomatic: control.get('isAutomatic')?.value ?? false,
      };
    });
  }

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar permiso',
      name,
      paragraph: `Está seguro(a) que desea eliminar el permiso`,
      warning: 'Esta acción no se puede deshacer.',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.removePermission(id);
      },
    });
  }

  /////////////// Submit events //////////////

  async onCreate() {
    try {
      await firstValueFrom(
        this.applicationService.create({
          ...this.formData,
          isActive: true,
          claims: this.formattedPermissions(),
        } as ApplicationCreate),
      );
      this.notification.success('Aplicación creada correctamente.');
      this.router.navigate(['/applications']);
    } catch (error) {
      this.notification.error(
        (error as Error).message || 'Error al crear la aplicación.',
      );
    }
  }

  async onUpdate() {
    try {
      await firstValueFrom(
        this.applicationService.update({
          ...this.formData,
          applicationId: this.applicationId,
          claims: this.formattedPermissions(),
        } as ApplicationCreate),
      );
      this.notification.success('Aplicación actualizada correctamente.');
      this.router.navigate(['/applications']);
    } catch (error) {
      this.notification.error(
        (error as Error).message || 'Error al actualizar la aplicación.',
      );
    }
  }

  async onSubmit() {
    try {
      if (!this.isValidForm()) return;
      this.isPending.set(true);
      if (this.isEdit) {
        this.onUpdate();
        return;
      }
      await this.onCreate();
    } catch (_error) {
      this.notification.error('Ocurrió un error al crear la aplicación.');
    } finally {
      this.isPending.set(false);
    }
  }
}
