import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { StepsForm } from '../../../components/ui';
import { steps as stepsConfig } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  InputComponent,
  CheckboxComponent,
  SearchComponent,
  SelectComponent,
  CustomSelectSearch,
} from '../../../components/shared';
import {
  OrganizationAdapted,
  OrganizationApplicationAdapted,
  PersonAdapted,
  PolicyAdapted,
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
  PersonService,
  PositionService,
  TokenDecoder,
  UserService,
} from '../../../services';
import { firstValueFrom } from 'rxjs';
import {
  noDefaultValidator,
  passwordMatchValidator,
} from '../../../validators';
import { NgClass } from '@angular/common';
import { RoleService } from '../../../services/role/role.service';
import { PolicyService } from '../../../services/policy/policy.service';
import { UserAppPolicyService } from '../../../services/user-app-policy/user-app-policy.service';

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
  selector: 'app-user-maintenance',
  imports: [
    ReactiveFormsModule,
    InputComponent,
    FontAwesomeModule,
    SearchComponent,
    CheckboxComponent,
    SelectComponent,
    StepsForm,
    NgClass,
    CustomSelectSearch
],
  templateUrl: './user-maintenance.html',
})
export class UserMaintenance implements OnInit {
  @ViewChild(StepsForm) stepsForm!: StepsForm;

  faTrash = faTrash;

  private layoutService = inject(LayoutService);
  private tokenDecoder = inject(TokenDecoder);
  faPen = faPen;
  faCheck = faCheck;

  protected permissions = this.tokenDecoder.getPermissions();
  protected isLocalAdmin = this.permissions.includes('admin_local');
  protected stepSequence: number[] = this.isLocalAdmin
    ? [1, 3, 4, 5]
    : [1, 2, 3, 4, 5];

  steps = {
    ...stepsConfig,
    steps: this.isLocalAdmin
      ? stepsConfig.steps.filter((step) => step.id !== 2)
      : stepsConfig.steps,
  };

  protected isLoading = signal(false);
  protected isLoadingUser = signal(false);
  protected isLoadingOrgApp = signal(false);
  protected isLoadingRole = signal(false);
  protected isLoadingPolicies = signal(false);
  protected isPending = signal(false);

  protected isFirstStepSubmitted = signal(false);

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
  protected availablePolicies = signal<PolicyAdapted[]>([]);
  protected selectedPolicyIds = signal<Set<string>>(new Set());
  protected policySearchQuery = signal('');
  protected isSearching = signal(false);
  protected positionsOptions = signal<Select[]>([]);
  private currentPositionOption = signal<Select | null>(null);
  protected positionsOptionsWithCurrent = computed(() => {
    const current = this.currentPositionOption();
    const opts = this.positionsOptions();
    if (!current) return opts;
    return opts.some((p) => String(p.value) === String(current.value))
      ? opts
      : [current, ...opts];
  });

  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private userService = inject(UserService);
  private personService = inject(PersonService);
  private organizationService = inject(OrganizationService);
  private orgAppService = inject(OrganizationApplicationService);
  private roleService = inject(RoleService);
  private notificationService = inject(NotificationService);
  private policyService = inject(PolicyService);
  private userAppPolicyService = inject(UserAppPolicyService);
  private positionService = inject(PositionService);


  private id = this.activatedRoute.snapshot.paramMap.get('id') as string;
  protected isEdit = !!this.id;

  fb = new FormBuilder();

  form = this.fb.group(
    {
      personnelId: [''],
      email: ['', [Validators.required, Validators.email]],
      positionId: [''],
      personPositionId: [''],
      mustChangePassword: [true],
      generateRandomPassword: [false],
      isGlobalAdmin: [false],
      organizationId: ['default', [Validators.required, noDefaultValidator]],
    },
    {
      validators: !this.isEdit ? passwordMatchValidator : null,
    },
  );

  ngOnInit(): void {
    this.layoutService.setPageTitle('Mantenimiento de Usuario');
    this.layoutService.breadcrumbs.set([
      { label: 'Usuarios', url: '/users' },
      { label: 'Mantenimiento', url: '/users/maintenance' },
    ]);
    this.getOrganizations();
    this.getAllPositions();
    if (!this.isEdit) return;
    this.getUserById();
  }

  /////////////////// Getters ////////////////////////////

  get currentStep() {
    const position = this.stepsForm?.steps().current ?? 1;
    return this.stepSequence[position - 1] ?? position;
  }

  get firstStep() {
    return this.currentStep === 1;
  }

  get finalStep() {
    return (this.stepsForm?.steps().current ?? 1) === this.stepSequence.length;
  }

  get organizationIdSelected() {
    return this.form.get('organizationId')?.value || null;
  }

  get formData(): UserCreate {
    return {
      userId: this.form.get('userId')?.value || '',
      personnelId: this.personData()?.id || '',
      organizationId: this.form.get('organizationId')?.value || '',
      personPositionId: this.personData()?.personnelId || '',
      positionId: this.form.get('positionId')?.value || '',
      email: this.form.get('email')?.value || '',
      userPassword: this.form.get('password')?.value || '',
      securityKey: this.form.get('securityKey')?.value || '',
      isServerAdmin: this.form.get('isGlobalAdmin')?.value || false,
      isDomainAuthenticated: true,
      mustChangePassword: this.form.get('mustChangePassword')?.value || false,
      isActive: this.userData()?.isActive || false,
      userApplications: this.getUserApplications(),
    };
  }

  get formDataUpdate(): Partial<UserCreate> {
    return {
      userId: this.id,
      personnelId: this.personData()?.id || '',
      email: this.form.get('email')?.value || '',
      positionId: this.form.get('positionId')?.value || '',
      personPositionId: this.form.get('personPositionId')?.value || '',
      securityKey: this.form.get('securityKey')?.value || '',
      isServerAdmin: this.form.get('isGlobalAdmin')?.value || false,
      isDomainAuthenticated: true,
      mustChangePassword: this.form.get('mustChangePassword')?.value || false,
      isActive: this.userData()?.isActive || false,
      userApplications: this.getUserApplications(),
      organizationId: this.form.get('organizationId')?.value || '',
    };
  }

  get password() {
    return this.form.get('password')?.value || '';
  }

  get confirmPassword() {
    return this.form.get('confirmPassword')?.value || '';
  }

  get email() {
    return this.form.get('email')?.value || '';
  }

  get isServerAdmin() {
    return this.form.get('isGlobalAdmin')?.value || '';
  }

  get mustChangePassword() {
    return this.form.get('mustChangePassword')?.value || '';
  }

  get positionId() {
    return this.form.get('positionId')?.value || '';
  }

  set positionId(value: string) {
    this.form.get('positionId')?.setValue(value);
  }

  get organizationId() {
    return this.form.get('organizationId')?.value || null;
  }

  ////////////////////// Fetching data /////////////////////////////

  async getUserById() {
    try {
      this.isLoadingUser.set(true);
      if (!this.id) return;
      const user = await firstValueFrom(this.userService.findOne(this.id));
      this.userData.set(user);
      this.loadFormData(user);
    } catch (_error) {
      this.notification.error('Error al consultar la información del usuario');
    } finally {
      this.isLoadingUser.set(false);
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
          ExcludePositionRoles : true
        }),
      );
      this.rolesData.set(roles.data);
    } catch (_error) {
      // this.notification.error(
      //   'Error al consultar los permisos de la aplicación',
      // );
    } finally {
      this.isLoadingRole.set(false);
    }
  }

  async getPersonByIdentification(identificationNumber: string) {
    this.currentPositionOption.set(null);
    try {
      const response = await firstValueFrom(
        this.personService.findByIdentificationNumber(identificationNumber),
      );
      this.personData.set(response);
      this.form.get('personPositionId')?.setValue(response.personnelId ?? '');
      this.form.get('positionId')?.setValue(response.position?.id || '');
      this.form.get('organizationId')?.setValue(response.organizationId || 'default');
      this.form.get('personnelId')?.setErrors(null);
      if (response.position) {
        this.currentPositionOption.set({ value: response.position.id, label: response.position.name });
      }
    } catch (err) {
      this.notificationService.error(
        (err as any)?.message || 'Error al consultar la persona',
      );
      this.personData.set(null);
    }
  }

  protected onSearchPositionsFilter(value: string | number): void {
    this.getAllPositions(String(value))
  }

   async getAllPositions(q  = '') {
    try {
      const posResponse = await firstValueFrom(this.positionService.search(q,{ pageSize: 200 }));
      const raw = (posResponse?.data ?? posResponse ?? []) as { id: string; name: string }[];
      this.positionsOptions.set(raw.map((p) => ({ value: p.id, label: p.name })));
    } catch (_error) {
      this.notificationService.error('Error al obtener los cargos');
    }
  }

  async getOrganizations(search?: string) {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.organizationService.findAll(search ? { search } : undefined),
      );
      this.organizationOptions.set(this.loadOrganizationOptions(response.data));
    } catch (_error) {
      this.notification.error('Error al consultar las organizaciones');
    } finally {
      this.isLoading.set(false);
    }
  }

  onOrgSearch(search: string) {
    this.getOrganizations(search);
  }

  onAppSearch(term: string) {
    this.loadAppsForSelectedOrgs(term);
  }

  onRoleSearch(term: string) {
    this.roleSearchTerm.set(term);
  }

  /////////////////// Helpers //////////////////////////

  getUserApplications(): Pick<
    UserAppCreate,
    | 'organizationApplicationId'
    | 'roleId'
    | 'isActive'
    | 'roleName'
    | 'policyIds'
  >[] {
    return (
      this.getApplicationSelected().map((app) => ({
        organizationApplicationId: app.organizationApplicationId,
        roleId: app.roleId,
        roleName: app.roleName,
        isActive: true,
        policyIds: [...this.selectedPolicyIds()],
      })) || []
    );
  }

  getCountOfSelectedRolesByApp(orgAppId: number): number {
    return this.selectedRolesByApp().has(orgAppId) ? 1 : 0;
  }

  getNameOfSelectedRolesByApp(orgAppId: number): string | undefined {
    const role = this.selectedRolesByApp().get(orgAppId);
    return role?.name;
  }

  getOrganizationName(): string | undefined {
    const organization = this.organizationOptions().find(
      (org) => String(org.value) === String(this.organizationId),
    );
    return organization?.label;
  }

  getPositionName(): string {
    const pos = this.positionsOptionsWithCurrent().find(
      (p) => String(p.value) === String(this.positionId),
    );
    return pos?.label || this.positionId;
  }

  getApplicationSelected(): ApplicationSelected[] {
    const applications = this.appDataCache();
    const appWithRolesSelected: ApplicationSelected[] = [];
    this.selectedRolesByApp().forEach((role, organizationApplicationId) => {
      const app = applications.get(organizationApplicationId);
      if (!app) return;
      appWithRolesSelected.push({
        organizationApplicationId: app?.organizationApplicationId ?? '',
        applicationName: app?.application?.name ?? '',
        applicationDescription: app?.application?.description ?? '',
        roleId: role?.id ?? 0,
        roleName: role?.name ?? '',
      });
    });
    return appWithRolesSelected;
  }

  getApplicationSelectedGroupedByOrg(): OrgApplicationGroup[] {
    const orgOptions = this.organizationOptions();
    const applications = this.appDataCache();
    const groups = new Map<string, OrgApplicationGroup>();
    this.selectedRolesByApp().forEach((role, organizationApplicationId) => {
      const app = applications.get(organizationApplicationId);
      if (!app) return;
      const orgId = String(app.organizationId);
      if (!groups.has(orgId)) {
        const orgName =
          orgOptions.find((o) => String(o.value) === orgId)?.label ?? '';
        groups.set(orgId, { orgId, orgName, apps: [] });
      }
      groups.get(orgId)!.apps.push({
        organizationApplicationId: app?.organizationApplicationId ?? '',
        applicationName: app?.application?.name ?? '',
        applicationDescription: app?.application?.description ?? '',
        roleId: role?.id ?? 0,
        roleName: role?.name ?? '',
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

  isValidFormForStep1() {
    this.isFirstStepSubmitted.set(true);
    this.form.get('organizationId')?.markAsTouched();
    this.form.get('email')?.markAsTouched();
    this.form.get('positionId')?.markAsTouched();
    this.form.get('password')?.markAsTouched();
    this.form.get('confirmPassword')?.markAsTouched();
  }

  async nextStep() {
    if (this.currentStep === 1) {
      if (!this.form.valid) {
        this.isValidFormForStep1();
        return;
      }
      if (this.isLocalAdmin) {
        const organizationId = this.organizationIdSelected;
        this.selectedOrganizationIds.set(
          organizationId ? [String(organizationId)] : [],
        );
        await this.loadAppsForSelectedOrgs();
      }
      this.stepsForm.nextStep();
    } else if (this.currentStep === 2) {
      if (this.selectedOrganizationIds().length === 0) {
        this.notification.error('Debe seleccionar al menos una organización.');
        return;
      }
      await this.loadAppsForSelectedOrgs();
      this.stepsForm.nextStep();
    } else if (this.currentStep === 3) {
      this.stepsForm.nextStep();
      await this.loadPolicies();
      if (this.isEdit) await this.loadExistingPolicies();
    } else {
      this.stepsForm.nextStep();
    }
  }

  previousStep() {
    this.stepsForm.previousStep();
  }

  goToStep(step: number) {
    this.stepsForm.goToStep(step);
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
    this.form.setValue({
      personnelId: data.personnelId || '',
      email: data.email || '',
      mustChangePassword: data.mustChangePassword || false,
      generateRandomPassword: false,
      isGlobalAdmin: data.isServerAdmin || false,
      organizationId: data.organizationId || 'default',
      positionId: data?.positionId || '',
      personPositionId:  data?.personPositionId || '',
    });
    if (data.positionId) {
      this.currentPositionOption.set({ value: data.positionId, label: data.position || data.positionId });
    }
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
    const defaultOption = {
      label: 'Seleccione una organización',
      value: 'default',
    };
    return [
      defaultOption,
      ...organization.map((item) => ({
        label: item.name,
        value: item.id.toString(),
      })),
    ];
  }

  resetForm() {
    this.form.setValue({
      personnelId: '',
      email: '',
      mustChangePassword: false,
      generateRandomPassword: false,
      isGlobalAdmin: false,
      organizationId: 'default',
      positionId: '',
      personPositionId : ''
    });
    this.selectedRolesByApp.set(new Map());
    this.selectedOrganizationIds.set([]);
    this.orgAppData.set([]);
    this.appDataCache.set(new Map());
    this.personData.set(null);
    this.currentPositionOption.set(null);
  }

  goBackToList() {
    this.router.navigate(['/users']);
  }

  isValidForm() {
    let valid = true;
    if (this.form.invalid) {
      this.notification.error(
        'Favor de completar todos los campos requeridos.',
      );
      valid = false;
    }

    if (this.password !== this.confirmPassword) {
      this.notification.error('Las contraseñas no coinciden.');
      valid = false;
    }

    return valid;
  }

  async onSearch(identificationNumber$: string) {
    this.isSearching.set(true);
    this.form.get('personnelId')?.setErrors(null);
    if (identificationNumber$ === '') {
      this.resetForm();
      this.isSearching.set(false);
      return;
    }
    await this.getPersonByIdentification(identificationNumber$);
    this.isSearching.set(false);
  }

  goToUsers() {
    this.router.navigate(['/users']);
  }

  /////////////////// Policy helpers //////////////////////////

  async loadPolicies() {
    try {
      this.isLoadingPolicies.set(true);
      const res = await firstValueFrom(
        this.policyService.findAllByOrganization(
          this.form.get('organizationId')?.getRawValue(),
        ),
      );
      this.availablePolicies.set(
        res.data.map((policy) => {
          const formatted = (policy.conditions || []).reduce(
            (acc, condition) => {
              acc[condition.conditionKey] = condition.conditionValue;
              return acc;
            },
            {} as Record<string, string>,
          );

          return {
            ...policy,
            conditionFormatted: formatted,
          };
        }),
      );
    } catch (_error) {
      this.notification.error('Error al cargar las políticas');
    } finally {
      this.isLoadingPolicies.set(false);
    }
  }

  async loadExistingPolicies() {
    try {
      const userApps = this.userData()?.userApplications;
      if (!userApps?.length) return;

      const associations = await firstValueFrom(
        this.userAppPolicyService.findAllByUserApp(
          userApps[0].userApplicationId,
        ),
      );

      const ids = new Set(
        associations.data
          .filter((a) => a.isActive === true)
          .map((a) => a.policyId),
      );
      this.selectedPolicyIds.set(ids);
    } catch (_error) {
      // silently ignore — no existing associations
    }
  }

  togglePolicySelection(policyId: string) {
    const current = new Set(this.selectedPolicyIds());
    if (current.has(policyId)) {
      current.delete(policyId);
    } else {
      current.add(policyId);
    }
    this.selectedPolicyIds.set(current);
  }

  isPolicySelected(policyId: string): boolean {
    return this.selectedPolicyIds().has(policyId);
  }

  getSelectedPolicies(): PolicyAdapted[] {
    const ids = this.selectedPolicyIds();
    return this.availablePolicies().filter((p) => ids.has(p.policyId));
  }

  /////////////////// Submit events //////////////////////////

  async onCreate() {
    try {
      await firstValueFrom(this.userService.create(this.formData));
      this.notification.success('Usuario creado correctamente.');
      this.goToUsers();
    } catch (error) {
      this.notification.error(
        (error as Error).message || 'Ocurrió un error al crear el usuario.',
      );
    }
  }

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
      if (!this.isValidForm()) return;
      this.isPending.set(true);

      if (this.isEdit) {
        await this.onUpdate();
      } else {
        await this.onCreate();
      }
    } catch (_error) {
      this.notification.error('Ocurrió un error al crear el usuario.');
    } finally {
      this.isPending.set(false);
    }
  }

 
}
