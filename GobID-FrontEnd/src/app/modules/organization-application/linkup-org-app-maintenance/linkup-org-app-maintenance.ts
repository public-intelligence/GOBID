import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faArrowLeft, faX, faCheck } from '@fortawesome/free-solid-svg-icons';
import { steps } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { SearchComponent } from '../../../components/shared/search/search';
import { ActivatedRoute, Router } from '@angular/router';
import {
  AccessTypeAdapted,
  ApplicationAccessAdapted,
  ApplicationAccessCreate,
  ApplicationAdapted,
  OrganizationApplicationAdapted,
  OrganizationApplicationCreate,
} from '../../../models';
import { CheckboxComponent } from '../../../components/shared';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import {
  ApplicationService,
  NotificationService,
  OrganizationApplicationService,
  accessTypeService,
  ApplicationAccessService,
} from '../../../services';
import { firstValueFrom } from 'rxjs';
import { NgClass } from '@angular/common';
import { atLeastOneTrueValidator } from '../../../validators';
import { LoadingSpinner } from '../../../components/ui/loading-spinner/loading-spinner';
import { StepsForm } from '../../../components/ui';
import { InfoApp } from '../info-app/info-app';

@Component({
  selector: 'app-linkup-apps-maintenance',
  imports: [
    FontAwesomeModule,
    CheckboxComponent,
    ReactiveFormsModule,
    SearchComponent,
    NgClass,
    LoadingSpinner,
    StepsForm,
    InfoApp,
  ],
  templateUrl: './linkup-org-app-maintenance.html',
})
export class LinkupOrgAppsMaintenance implements OnInit {
  @ViewChild(StepsForm) stepsForm!: StepsForm;

  private accessTypeService = inject(accessTypeService);
  private applicationService = inject(ApplicationService);
  private applicationAccessService = inject(ApplicationAccessService);
  private notification = inject(NotificationService);
  private organizationApplicationService = inject(
    OrganizationApplicationService,
  );
  private route = inject(ActivatedRoute);
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private organizationApplications =
    signal<OrganizationApplicationAdapted | null>(null);
  private applicationAccess = signal<
    ApplicationAccessAdapted[] | ApplicationAccessCreate[]
  >([]);

  protected faArrowLeft = faArrowLeft;
  protected faX = faX;

  private layoutService = inject(LayoutService);
  protected faCheck = faCheck;
  protected accessType = signal<AccessTypeAdapted[]>([]);
  protected id = this.route.snapshot.paramMap.get('id');
  protected idApp = this.route.snapshot.paramMap.get('idApp');
  protected application = signal<ApplicationAdapted | null>(null);
  protected accessTypeForm = this.fb.group(
    {},
    { asyncValidators: atLeastOneTrueValidator() },
  );
  protected applications = signal<ApplicationAdapted[]>([]);
  protected valueSearched = signal<string>('');
  protected isLoading = signal(false);
  protected applicationBound = signal<OrganizationApplicationAdapted[] | null>(
    null,
  );
  protected steps = steps;
  protected buttonText = signal('Siguiente Paso');
  protected searched = signal(false);

  get currentStep() {
    return this.stepsForm?.steps().current ?? 1;
  }

  get firstStep() {
    return this.currentStep === 1;
  }

  get finalStep() {
    return this.currentStep === this.steps.steps.length;
  }

  get formGroup() {
    return Object.entries(this.accessTypeForm.value);
  }

  async ngOnInit() {
    this.layoutService.setPageTitle('Vincular Aplicación');
    this.layoutService.breadcrumbs.set([
      {
        label: 'Vinculación Organizacion-Aplicaciones',
        url: 'organizations-application',
      },
      {
        label: 'Aplicaciones Vinculadas',
        url: `organization-application/organization/${this.id}`,
      },
      {
        label: 'Vincular Aplicación',
        url: `organization-application/organization/${this.id}/linkup-app/${this.idApp}`,
      },
    ]);
    this.getAccessType();
    if (this.id && this.idApp) {
      await this.setValue(this.id, this.idApp);
    } else {
      await this.getApplication();
    }
  }

  async onSubmit() {
    const appOrg: OrganizationApplicationCreate = {
      organizationId: Number(this.id) || 0,
      applicationId: this.application()?.id || 0,
      isActive: true,
    };
    if (this.accessTypeForm.invalid) {
      return;
    }
    try {
      if (!this.idApp) {
        await firstValueFrom(
          this.organizationApplicationService.upsert(appOrg),
        );
      }
      await this.getOrgAppById(
        appOrg.organizationId.toString(),
        appOrg.applicationId.toString(),
      );

      await this.setAppAccess();

      this.notification.success(
        this.idApp
          ? 'Aplicación actualizada correctamente'
          : 'Aplicación vinculada correctamente',
      );
      this.onGoBack();
    } catch (_error) {
      this.notification.error(
        this.idApp
          ? 'Error actualizando aplicación'
          : 'Error vinculando aplicación',
      );
    }
  }

  /**
   *  Obtiene los tipos de acceso
   */
  getAccessType = async () => {
    try {
      const response = await firstValueFrom(this.accessTypeService.findAll());
      this.accessType.set(response.data);
      this.accessType().map((item) => {
        this.accessTypeForm.addControl(item.name, this.fb.control(false));
      });
    } catch (_error) {
      this.notification.error('Error al obtener los tipos de acceso');
    }
  };

  /**
   *  Obtiene las aplicaciones
   */
  getApplication = async (search?: string) => {
    this.isLoading.set(true);
    try {
      const response = await firstValueFrom(
        this.applicationService.findAll({ search: search }),
      );
      const boundApps = await this.getOrganizationApps();

      const boundAppIds = boundApps.map((app) => app.applicationId);

      const unlinkedApps = response.data.filter(
        (app) => !boundAppIds.includes(app.id),
      );
      this.applications.set(unlinkedApps);
      if (search) this.valueSearched.set(search);
      if (unlinkedApps.length === 0) this.searched.set(true);
      else this.searched.set(false);
    } catch (_error) {
      this.notification.error('Error al obtener la aplicación');
    } finally {
      this.isLoading.set(false);
    }
  };

  /**
   *  Obtiene las aplicaciones
   */
  getOrgAppById = async (idOrg: string, idApp: string) => {
    try {
      const response = await firstValueFrom(
        this.organizationApplicationService.findAllByApplicationOrganizationId(
          idOrg,
          idApp,
        ),
      );
      this.organizationApplications.set(response.data[0]);
    } catch (_error) {
      this.notification.error(
        'Error al obtener las aplicaciones vinculadas a la organización',
      );
    }
  };

  getOrganizationApps = async (): Promise<OrganizationApplicationAdapted[]> => {
    try {
      const response = await firstValueFrom(
        this.organizationApplicationService.findAllByOrganizationId(
          this.id ?? '',
        ),
      );
      return response.data;
    } catch (_error) {
      return [];
    }
  };

  /**
   *  Establece los accesos de la aplicación
   */
  setAppAccess = async () => {
    this.isLoading.set(true);
    try {
      if (this.idApp) {
        this.editAccess();
      } else {
        this.newAccess();
      }
      await Promise.all(
        this.applicationAccess().map((orgApp) =>
          firstValueFrom(this.applicationAccessService.upsert(orgApp)),
        ),
      );
    } catch (_error) {
      this.notification.error('Error al guardar los cambios');
    } finally {
      this.isLoading.set(false);
    }
  };

  /**
   *  Obtiene los accesos de una aplicación
   * entre una aplicación y su organización
   * @returns los accesos de la aplicación
   */
  getAppAccess = async (): Promise<ApplicationAccessAdapted[] | null> => {
    try {
      const response = await firstValueFrom(
        this.applicationAccessService.findOneByOrgAppId(
          this.organizationApplications()?.organizationApplicationId.toString() ||
            '',
        ),
      );
      return response.data;
    } catch (_error) {
      this.notification.error('Error al obtener acceso a la aplicación');
    }

    return null;
  };

  /**
   *  Busca una aplicación por su nombre o codigo
   */
  onSearch = async ($event: string) => {
    if (!$event) {
      this.applications.set([]);
      this.application.set(null);
      this.searched.set(false);
      this.getApplication();
      return;
    }
    this.getApplication($event);
  };

  newAccess = async () => {
    const applicationAccess: ApplicationAccessCreate[] = [];
    this.accessType().map((type) => {
      if (this.accessTypeForm.get(type.name)?.value) {
        applicationAccess.push({
          organizationApplicationId:
            this.organizationApplications()?.organizationApplicationId.toString() ||
            '',
          accessTypeId: type.id,
          isActive: this.accessTypeForm.get(type.name)?.value,
          configurationJson: JSON.stringify({}),
        });
      }
    });
    this.applicationAccess.set(applicationAccess);
  };

  editAccess = async () => {
    const applicationAccess: ApplicationAccessCreate[] = [];
    const existingAccess = this.applicationAccess();
    const accessTypes = this.accessType();

    accessTypes.forEach((type) => {
      const currentValue = this.accessTypeForm.get(type.name)?.value;
      const existing = existingAccess.find(
        (access) => access.accessTypeId === type.id,
      );

      if (existing) {
        if (existing.isActive !== currentValue) {
          applicationAccess.push({
            applicationAccessId: existing.applicationAccessId,
            organizationApplicationId:
              this.organizationApplications()?.organizationApplicationId.toString() ||
              '',
            accessTypeId: type.id,
            isActive: currentValue,
            configurationJson: JSON.stringify({}),
          });
        }
      } else if (currentValue) {
        applicationAccess.push({
          organizationApplicationId:
            this.organizationApplications()?.organizationApplicationId.toString() ||
            '',
          accessTypeId: type.id,
          isActive: currentValue,
          configurationJson: JSON.stringify({}),
        });
      }
    });
    this.applicationAccess.set(applicationAccess);
  };

  /**
   *  Edita los tipos de acceso de una aplicación
   * @idOrg id de la organización
   * @idApp id de la aplicación
   */
  setValue = async (idOrg: string, idApp: string) => {
    this.isLoading.set(true);
    try {
      await this.getOrgAppById(idOrg, idApp);
      const appAccess = await this.getAppAccess();
      this.applicationAccess.set(appAccess ?? []);
      this.application.set(
        this.organizationApplications()?.application ?? null,
      );
      appAccess?.map(async (access) => {
        this.accessType().find((type) => {
          if (type.id === access.accessTypeId)
            this.accessTypeForm.get(type.name)?.setValue(access.isActive);
        });
      });
      this.goToStep(2);
    } catch (_error) {
      this.notification.error('Error al obtener los datos de la aplicación');
    } finally {
      this.isLoading.set(false);
    }
  };

  cancelAssignation() {
    this.application.set(null);
  }

  /**
   *  Selecciona una aplicación
   * @param app la aplicación a seleccionar
   */
  selectApplication(app: ApplicationAdapted) {
    this.application.set(app);
    this.stepsForm.nextStep();
  }

  nextStep() {
    switch (this.currentStep) {
      case 1:
        this.stepsForm.nextStep();
        break;
      case 2:
        if (this.accessTypeForm.invalid) {
          this.notification.error(
            'Debe seleccionar al menos un tipo de acceso',
          );
          return;
        }
        this.stepsForm.nextStep();
        this.buttonText.set(this.idApp ? 'Guardar Cambios' : 'Registrar');
        break;
      case 3:
        this.onSubmit();
    }
  }

  previousStep() {
    switch (this.currentStep) {
      case 1:
        this.onGoBack();
        break;
      case 2:
        if (this.idApp) {
          this.onGoBack();
          return;
        }
        this.cancelAssignation();
        this.accessTypeForm.reset();
        this.stepsForm.previousStep();
        break;
      case 3:
        this.buttonText.set('Siguiente Paso');
        this.stepsForm.previousStep();
    }
  }

  goToStep(step: number) {
    this.stepsForm.goToStep(step);
  }

  onGoBack() {
    this.router.navigate([`/organization-application/organization/${this.id}`]);
  }
}
