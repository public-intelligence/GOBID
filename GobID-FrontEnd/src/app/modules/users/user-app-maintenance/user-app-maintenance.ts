import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { StepsForm } from '../../../components/ui';
import { SearchComponent } from '../../../components/shared/search/search';
import { steps } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { ReactiveFormsModule } from '@angular/forms';
import {
  OrganizationAdapted,
  OrganizationApplicationAdapted,
  PersonAdapted,
  RoleAdapted,
  Select,
  UserAdapted,
  UserAppCreate,
  UserCreate,
} from '../../../models';
import { faCheck, faPen, faTrash } from '@fortawesome/free-solid-svg-icons';
import { ActivatedRoute, Router } from '@angular/router';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  NotificationService,
  OrganizationApplicationService,
  OrganizationService,
  UserService,
} from '../../../services';
import { firstValueFrom } from 'rxjs';
import { RoleService } from '../../../services/role/role.service';
import { NgClass } from '@angular/common';

interface ApplicationSelected {
  organizationApplicationId: number;
  applicationName: string;
  applicationDescription?: string;
  roleId: number;
  roleName: string;
}

interface OrgApplicationGroup {
  orgId: string;
  orgName: string;
  apps: ApplicationSelected[];
}

@Component({
  selector: 'app-user-app-maintenance',
  imports: [
    ReactiveFormsModule,
    FontAwesomeModule,
    NgClass,
    StepsForm,
    SearchComponent,
  ],
  templateUrl: './user-app-maintenance.html',
})
export class UserAppMaintenance implements OnInit {
  @ViewChild(StepsForm) stepsForm!: StepsForm;

  faTrash = faTrash;

  private layoutService = inject(LayoutService);
  faPen = faPen;
  faCheck = faCheck;
  steps = steps;

  protected isLoading = signal(false);
  protected isLoadingOrgApp = signal(false);
  protected isLoadingRole = signal(false);
  protected isPending = signal(false);

  protected personData = signal<PersonAdapted | null>(null);
  protected userData = signal<UserAdapted | null>(null);
  protected orgAppData = signal<OrganizationApplicationAdapted[]>([]);
  protected appDataCache = signal<Map<number, OrganizationApplicationAdapted>>(
    new Map(),
  );
  protected organizationOptions = signal<Select[]>([]);
  protected selectedOrganizationIds = signal<string[]>([]);
  protected selectedApp = signal<number | null>(null);
  protected selectedRolesByApp = signal<Map<number, Partial<RoleAdapted>>>(
    new Map(),
  );
  protected rolesData = signal<RoleAdapted[]>([]);
  protected roleSearchTerm = signal('');
  protected filteredRolesData = computed(() => {
    const term = this.roleSearchTerm().trim().toLowerCase();
    if (!term) return this.rolesData();
    return this.rolesData().filter((role) =>
      role.name.toLowerCase().includes(term),
    );
  });

  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private userService = inject(UserService);
  private organizationService = inject(OrganizationService);
  private orgAppService = inject(OrganizationApplicationService);
  private roleService = inject(RoleService);

  private id = this.activatedRoute.snapshot.paramMap.get('id') as string;
  protected isEdit = !!this.id;

  async ngOnInit() {
    this.layoutService.setPageTitle('Vinculación de Aplicaciones');
    this.layoutService.breadcrumbs.set([
      { label: 'Usuarios', url: '/users' },
      { label: 'Aplicaciones', url: `users/link/${this.id}` },
    ]);
    this.isLoading.set(true);
    await this.getUserById();
    await this.getOrganizations();
    this.isLoading.set(false);
  }

  /////////////////// Step navigation ////////////////////////////

  get currentStep() {
    return this.stepsForm?.steps().current ?? 1;
  }

  get firstStep() {
    return this.currentStep === 1;
  }

  get finalStep() {
    return this.currentStep === this.steps.steps.length;
  }

  async nextStep() {
    if (this.currentStep === 1) {
      if (this.selectedOrganizationIds().length === 0) {
        this.notification.error('Debe seleccionar al menos una organización.');
        return;
      }
      await this.loadAppsForSelectedOrgs();
    }
    this.stepsForm.nextStep();
  }

  previousStep() {
    this.stepsForm.previousStep();
  }

  /////////////////// Getters ////////////////////////////

  get formDataUpdate(): Partial<UserCreate> {
    return {
      userId: this.id,
      personnelId: this.personData()?.id || '',
      email: this.userData()?.email || '',
      position: this.userData()?.position || '',
      securityKey: this.userData()?.securityKey || '',
      isServerAdmin: this.userData()?.isServerAdmin || false,
      isDomainAuthenticated: true,
      mustChangePassword: this.userData()?.mustChangePassword || false,
      isActive: this.userData()?.isActive || false,
      userApplications: this.getUserApplications(),
      organizationId: this.userData()?.organizationId || '',
    };
  }

  ////////////////////// Fetching data /////////////////////////////

  async getUserById() {
    try {
      if (!this.id) return;
      const user = await firstValueFrom(this.userService.findOne(this.id));
      this.userData.set(user);
      this.loadFormData(user);
    } catch (_error) {
      this.notification.error('Error al consultar la información del usuario');
    }
  }

  async loadAppsForSelectedOrgs(search?: string) {
    try {
      this.isLoadingOrgApp.set(true);
      const orgIds = this.selectedOrganizationIds();
      const allApps: OrganizationApplicationAdapted[] = [];
      for (const orgId of orgIds) {
        const orgApp = await firstValueFrom(
          this.orgAppService.findAllByOrganizationId(
            orgId,
            search ? { search } : undefined,
          ),
        );
        allApps.push(...orgApp.data);
      }
      this.orgAppData.set(allApps);
      this.appDataCache.update((cache) => {
        const next = new Map(cache);
        allApps.forEach((app) => next.set(app.organizationApplicationId, app));
        return next;
      });
    } catch (_error) {
      this.notification.error(
        'Error al consultar las aplicaciones de la organización',
      );
    } finally {
      this.isLoadingOrgApp.set(false);
    }
  }

  async getApplicationRoles(orgAppId: number) {
    try {
      this.isLoadingRole.set(true);
      const roles = await firstValueFrom(
        this.roleService.findAll({
          OrganizationApplicationId: orgAppId,
          ExcludePositionRoles: true,
        }),
      );
      this.rolesData.set(roles.data);
    } catch (_error) {
      // silent
    } finally {
      this.isLoadingRole.set(false);
    }
  }

  async getOrganizations(search?: string) {
    try {
      const response = await firstValueFrom(
        this.organizationService.findAll(search ? { search } : undefined),
      );
      this.organizationOptions.set(this.loadOrganizationOptions(response.data));
    } catch (_error) {
      this.notification.error('Error al consultar las organizaciones');
    }
  }

  onOrgSearch(search: string) {
    this.getOrganizations(search);
  }

  /////////////////// Helpers //////////////////////////

  getUserApplications(): Pick<
    UserAppCreate,
    'organizationApplicationId' | 'roleId' | 'isActive' | 'roleName'
  >[] {
    return (
      this.getApplicationSelected().map((app) => ({
        organizationApplicationId: app.organizationApplicationId,
        roleId: app.roleId,
        roleName: app.roleName,
        isActive: true,
      })) || []
    );
  }

  getNameOfSelectedRolesByApp(orgAppId: number): string | undefined {
    const role = this.selectedRolesByApp().get(orgAppId);
    return role?.name;
  }

  getApplicationSelected(): ApplicationSelected[] {
    const applications = this.appDataCache();
    const appWithRolesSelected: ApplicationSelected[] = [];
    this.selectedRolesByApp().forEach((role, organizationApplicationId) => {
      const app = applications.get(organizationApplicationId);
      if (!app) return;
      appWithRolesSelected.push({
        organizationApplicationId: app?.organizationApplicationId ?? 0,
        applicationName: app?.application?.name ?? '',
        applicationDescription: app?.application?.description ?? '',
        roleId: role?.id ?? 0,
        roleName: role?.name ?? '',
      });
    });
    return appWithRolesSelected;
  }

  getApplicationSelectedGroupedByOrg(): OrgApplicationGroup[] {
    const applications = this.appDataCache();
    const groups = new Map<string, OrgApplicationGroup>();
    this.selectedRolesByApp().forEach((role, organizationApplicationId) => {
      const app = applications.get(organizationApplicationId);
      if (!app) return;
      const orgId = String(app.organizationId);
      if (!groups.has(orgId)) {
        const orgName =
          this.organizationOptions().find((o) => String(o.value) === orgId)
            ?.label ?? orgId;
        groups.set(orgId, { orgId, orgName, apps: [] });
      }
      groups.get(orgId)!.apps.push({
        organizationApplicationId: app.organizationApplicationId,
        applicationName: app.application?.name ?? '',
        applicationDescription: app.application?.description ?? '',
        roleId: role.id ?? 0,
        roleName: role.name ?? '',
      });
    });
    return Array.from(groups.values());
  }

  isRoleSelected(role: RoleAdapted): boolean {
    const selectedRole = this.selectedRolesByApp().get(
      role.organizationApplicationId,
    );
    return selectedRole?.id === role.id;
  }

  toggleOrganizationSelection(orgId: string) {
    const current = this.selectedOrganizationIds();
    const idx = current.indexOf(orgId);
    if (idx >= 0) {
      this.selectedOrganizationIds.set(current.filter((id) => id !== orgId));
    } else {
      this.selectedOrganizationIds.set([...current, orgId]);
    }
  }

  isOrganizationSelected(orgId: string): boolean {
    return this.selectedOrganizationIds().includes(orgId);
  }

  groupedOrgAppData = computed(() => {
    const apps = this.orgAppData();
    const orgOptions = this.organizationOptions();
    const groups = new Map<
      string,
      { orgId: string; orgName: string; apps: OrganizationApplicationAdapted[] }
    >();
    apps.forEach((app) => {
      const orgId = String(app.organizationId);
      if (!groups.has(orgId)) {
        const orgName =
          orgOptions.find((o) => String(o.value) === orgId)?.label ?? '';
        groups.set(orgId, { orgId, orgName, apps: [] });
      }
      groups.get(orgId)!.apps.push(app);
    });
    return Array.from(groups.values());
  });

  onAppSearch(term: string) {
    this.loadAppsForSelectedOrgs(term);
  }

  onRoleSearch(term: string) {
    this.roleSearchTerm.set(term);
  }

  handleAppSelect(appId: number) {
    this.selectedApp.set(appId);
    this.roleSearchTerm.set('');
    this.getApplicationRoles(appId);
  }

  handleRoleSelect(role: RoleAdapted) {
    const currentMap = new Map(this.selectedRolesByApp());
    const appId = role.organizationApplicationId;

    if (currentMap.has(appId) && currentMap.get(appId)?.id === role.id) {
      currentMap.delete(appId);
    } else {
      currentMap.set(appId, role);
    }

    this.selectedRolesByApp.set(currentMap);
  }

  loadFormData(data: UserAdapted) {
    this.personData.set(data.personnel);
    if (data.userApplications && data.userApplications.length > 0) {
      const orgsId = data.userApplications
        .map((app) => String(app.organizationId))
        .filter((value, index, self) => self.indexOf(value) === index);

      this.selectedOrganizationIds.set(orgsId);
    }
    const rolesMap = new Map<number, Partial<RoleAdapted>>();
    if (data.userApplications) {
      data.userApplications.forEach((app) => {
        rolesMap.set(app.organizationApplicationId, {
          id: app.roleId,
          name: app.roleName,
          organizationApplicationId: app.organizationApplicationId,
        });
      });
    }
    this.selectedRolesByApp.set(rolesMap);
  }

  loadOrganizationOptions(organization: OrganizationAdapted[]): Select[] {
    return organization.map((item) => ({
      label: item.name,
      value: item.id.toString(),
    }));
  }

  goBackToList() {
    this.router.navigate(['/users']);
  }

  goToUsers() {
    this.router.navigate(['/users']);
  }

  /////////////////// Submit events //////////////////////////

  async onUpdate() {
    try {
      await firstValueFrom(
        this.userService.update(this.id, this.formDataUpdate),
      );
      this.notification.success('Usuario actualizado correctamente.');
      this.goToUsers();
    } catch (error) {
      this.notification.error(
        (error as Error).message ||
          'Ocurrió un error al actualizar el usuario.',
      );
    }
  }

  async onSubmit() {
    try {
      this.isPending.set(true);
      await this.onUpdate();
    } catch (_error) {
      this.notification.error('Ocurrió un error al actualizar el usuario.');
    } finally {
      this.isPending.set(false);
    }
  }
}
