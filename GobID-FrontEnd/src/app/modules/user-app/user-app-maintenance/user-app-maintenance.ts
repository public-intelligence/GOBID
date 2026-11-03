import { Component, inject, OnInit, signal } from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { LayoutService } from '@public-intelligence/ui-core';
import {
  NotificationService,
  OrganizationApplicationService,
  OrganizationService,
  UserAppService,
  UserService,
} from '../../../services';
import {
  OrganizationAdapted,
  OrganizationApplicationAdapted,
  Select,
  UserAdapted,
  UserAppAdapted,
} from '../../../models';
import { CheckboxComponent, SelectComponent } from '../../../components/shared';
import { BasicDetail } from '../../../components/ui/basic-detail/basic-detail';
import { NothingFound } from '../../../components/ui/nothing-found/nothing-found';
import { LoadingSpinner } from '../../../components/ui/loading-spinner/loading-spinner';

interface AppCard {
  selected: boolean;
  organizationApplicationId: string;
  title: string;
  description: string;
  isActive: boolean;
  searchRole: string;
  dateAssingAccess: string;
  organizationName: string;
  selectedRole: string;
  roles: {
    id: string;
    name: string;
  }[];
}

@Component({
  selector: 'app-user-app-maintenance',
  imports: [
    ReactiveFormsModule,
    CheckboxComponent,
    SelectComponent,
    BasicDetail,
    NothingFound,
    LoadingSpinner,
  ],
  templateUrl: './user-app-maintenance.html',
})
export class UserAppMaintenance implements OnInit {
  notAvailableMessage = '[No disponible]';

  private layoutService = inject(LayoutService);

  protected organizationData = signal<OrganizationAdapted[] | null>(null);
  protected organizationOptions = signal<Select[]>([]);
  protected userData = signal<UserAdapted | null>(null);
  protected userAppData = signal<UserAppAdapted[]>({} as UserAppAdapted[]);
  protected appOrgData = signal<OrganizationApplicationAdapted[]>([]);
  protected isLoading = signal<boolean>(false);

  private router = inject(Router);
  private notification = inject(NotificationService);
  private organizationService = inject(OrganizationService);
  private orgAppService = inject(OrganizationApplicationService);
  private userService = inject(UserService);
  private userAppService = inject(UserAppService);
  private route = inject(ActivatedRoute);

  private userId = this.route.snapshot.paramMap.get('userId') as string;

  private fb = new FormBuilder();

  form = this.fb.group({
    organizationId: this.fb.control<string | null>(
      'default',
      Validators.required,
    ),
    selectAllRows: this.fb.control<boolean>(false),
    appCards: this.fb.array([]),
    selectedRole: this.fb.control<string>(''), // Add this line
  });

  get appCards() {
    return this.form.get('appCards') as FormArray;
  }

  async ngOnInit() {
    this.layoutService.setPageTitle('Vincular Aplicación de Usuario');
    this.layoutService.breadcrumbs.set([
      { label: 'Relación Usuario-Aplicación', url: '/user-application' },
      { label: 'Vincular', url: '/user-application/create' },
    ]);
    this.getUserInfo();
    await this.getUserApps(this.userId);
    this.getOrganizations();
    this.getOrganizationApplications();
  }

  ////////////// getters /////////////////////
  get isRowSelected() {
    return this.appCards.controls.some(
      (control) => control.get('selected')?.value,
    );
  }

  get isAllRowSelected() {
    return this.form.get('selectAllRows')?.value;
  }

  ////////////// services /////////////////////

  async getOrganizations() {
    try {
      const organizations = await firstValueFrom(
        this.organizationService.findAll(),
      );
      this.organizationData.set(organizations.data);
      this.organizationOptions.set(
        this.formattedOrganizationOptions(organizations.data),
      );
    } catch (_error) {
      this.notification.error('Error al cargar las organizaciones');
    }
  }

  async getUserInfo() {
    try {
      const user = await firstValueFrom(this.userService.findOne(this.userId));
      this.userData.set(user);
    } catch (_error) {
      this.notification.error('Error al cargar la información del usuario');
    }
  }

  async getUserApps(userId: string) {
    try {
      const user = await firstValueFrom(this.userAppService.findAll(userId));
      this.userAppData.set(user.data);
    } catch (_error) {
      this.notification.error('Error al cargar la información del usuario');
    }
  }

  async getOrganizationApplications() {
    try {
      this.isLoading.set(true);
      this.resetAppCard();
      const applications = await firstValueFrom(this.orgAppService.findAll());

      this.appOrgData.set(applications.data);
      applications.data.forEach((app) => {
        const userApp = this.findUserApp(app.organizationApplicationId);

        this.addAppCard({
          selected: false,
          organizationApplicationId: app.organizationApplicationId.toString(),
          title: app.application.name,
          description: app.application.description,
          organizationName: app.organization.name,
          isActive: userApp?.isActive || false,
          searchRole: '',
          dateAssingAccess: '',
          selectedRole: '',
          roles: app?.role.map((role) => {
            return {
              id: role.id.toString(),
              name: role.name,
            };
          }),
        });
      });
    } catch (_error) {
      this.notification.error(
        'Error al cargar las aplicaciones de la organización',
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  async getOrganizationApplicationsById(organizationId: string) {
    try {
      this.isLoading.set(true);
      this.resetAppCard();
      const applications = await firstValueFrom(
        this.orgAppService.findAllByOrganizationId(organizationId),
      );

      this.appOrgData.set(applications.data);

      applications.data.forEach((app) => {
        const userApp = this.findUserApp(app.organizationApplicationId);
        this.addAppCard({
          selected: false,
          organizationApplicationId: app.organizationApplicationId.toString(),
          title: app.application.name,
          description: app.application.description,
          organizationName: this.findOrganizationName() ?? '',
          isActive: userApp?.isActive || false,
          searchRole: '',
          dateAssingAccess: '',
          selectedRole: '',
          roles: [],
        });
      });
    } catch (_error) {
      this.notification.error(
        'Error al cargar las aplicaciones de la organización',
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  /////////////// helpers ///////////////////////

  getRolesControls(app: AbstractControl): AbstractControl[] {
    const roles = app.get('roles');
    return roles && roles instanceof FormArray ? roles.controls : [];
  }

  selectAllRows() {
    this.appCards.controls.forEach((control) => {
      control.get('selected')?.setValue(!this.isAllRowSelected);
    });
  }

  activateSelectedRows() {
    this.appCards.controls.forEach((control) => {
      if (control.get('selected')?.value) {
        control.get('isActive')?.setValue(true);
      }
    });
  }

  disableSelectedRows() {
    this.appCards.controls.forEach((control) => {
      if (control.get('selected')?.value) {
        control.get('isActive')?.setValue(false);
      }
    });
  }

  handleSelectRole(value: Event) {
    const selectElement = value.target as HTMLSelectElement;
    const selectedRole = selectElement.value as string;
    this.form.get('selectedRole')?.setValue(selectedRole);
  }

  goBack() {
    this.router.navigate(['/user-application']);
  }

  addAppCard(data: AppCard) {
    const appCard = this.fb.group({
      organizationApplicationId: this.fb.control(
        data.organizationApplicationId,
      ),
      selected: this.fb.control(data.selected),
      title: this.fb.control(data.title),
      description: this.fb.control(data.description),
      organizationName: this.fb.control(data.organizationName),
      isActive: this.fb.control(data.isActive),
      searchRole: this.fb.control(data.searchRole),
      dateAssingAccess: this.fb.control(data.dateAssingAccess),
      roles: this.fb.array(
        data.roles.map((role) =>
          this.fb.group({
            id: this.fb.control(role.id),
            name: this.fb.control(role.name),
          }),
        ),
      ),
    });
    this.appCards.push(appCard);
  }

  resetAppCard() {
    this.appCards.clear();
  }

  formattedOrganizationOptions(data: OrganizationAdapted[] | null): Select[] {
    if (!data) return [];
    const options: Select[] = [
      {
        value: 'default',
        label: 'Seleccione una organización',
      },
      ...data.map((option) => ({
        value: option.id.toString(),
        label: option.name,
      })),
    ];
    return options;
  }

  handleSelectOrganization(value: string) {
    if (value === 'default') {
      this.getOrganizationApplications();
    } else {
      this.getOrganizationApplicationsById(value);
    }
  }

  findUserApp(organizationApplicationId: number) {
    return this.userAppData().find(
      (app) =>
        Number(app.organizationApplicationId) ===
        Number(organizationApplicationId),
    );
  }

  findOrganizationName() {
    return this.organizationOptions().find(
      (org) =>
        org.value === (this.form.get('organizationId')?.value ?? 'default'),
    )?.label;
  }

  /////////////// submit actions ///////////////////////

  async onSubmit() {
    try {
      const values = this.appCards.value;
      values.forEach(async (value: AppCard) => {
        const userApp = this.findUserApp(
          Number(value.organizationApplicationId),
        );
        if (value.isActive) {
          await firstValueFrom(
            this.userAppService.create({
              userApplicationId: userApp?.id ?? '',
              userId: this.userId,
              organizationApplicationId: Number(
                value.organizationApplicationId,
              ),
              userApplicationDescription: '',
              isApplicationAdmin: true,
              isActive: true,
            }),
          );
          return;
        }

        if (userApp?.isActive && !value.isActive) {
          await firstValueFrom(
            this.userAppService.create({
              userApplicationId: userApp?.id ?? '',
              userId: this.userId,
              organizationApplicationId: Number(
                value.organizationApplicationId,
              ),
              userApplicationDescription: '',
              isApplicationAdmin: true,
              isActive: false,
            }),
          );
          return;
        }
      });

      this.router.navigate(['/user-application']);
      this.notification.success('Cambios guardados correctamente');
    } catch (_error) {
      this.notification.error('Error al guardar los cambios');
    }
  }
}
