import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { LayoutService } from '@public-intelligence/ui-core';
import {
  faChevronDown,
  faChevronUp,
  faPlus,
  faTrash,
  faArrowLeft,
  faSpinner,
} from '@fortawesome/free-solid-svg-icons';
import {
  OrganizationApplicationAdapted,
  PersonAdapted,
  PolicyAdapted,
  UserAdapted,
} from '../../../models';
import { ActivatedRoute, Router } from '@angular/router';
import {
  NotificationService,
  OrganizationApplicationService,
  UserService,
} from '../../../services';
import { PolicyService } from '../../../services/policy/policy.service';
import { UserAppPolicyService } from '../../../services/user-app-policy/user-app-policy.service';
import { firstValueFrom } from 'rxjs';
import { NgClass } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';

interface UserAppEntry {
  userApplicationId: string;
  organizationApplicationId: number;
  organizationId: string;
  organizationName: string;
  applicationName: string;
  roleName: string;
}

@Component({
  selector: 'app-user-policy-maintenance',
  imports: [NgClass, FontAwesomeModule, ConfirmationModalComponent],
  templateUrl: './user-policy-maintenance.html',
})
export class UserPolicyMaintenance implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  faChevronDown = faChevronDown;

  private layoutService = inject(LayoutService);
  faChevronUp = faChevronUp;
  faPlus = faPlus;
  faTrash = faTrash;
  faArrowLeft = faArrowLeft;
  faSpinner = faSpinner;

  protected isLoadingUser = signal(false);

  protected personData = signal<PersonAdapted | null>(null);
  protected userData = signal<UserAdapted | null>(null);
  protected userApps = signal<UserAppEntry[]>([]);

  protected expandedAppId = signal<string | null>(null);
  protected loadingAppId = signal<string | null>(null);
  protected pendingPolicyId = signal<string | null>(null);
  private appPoliciesMap = signal<Map<string, Map<string, string>>>(new Map());
  private appAvailablePoliciesMap = signal<Map<string, PolicyAdapted[]>>(
    new Map(),
  );
  private loadedAppIds = signal<Set<string>>(new Set());
  private orgPoliciesCache = new Map<string, PolicyAdapted[]>();

  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private userService = inject(UserService);
  private policyService = inject(PolicyService);
  private userAppPolicyService = inject(UserAppPolicyService);
  private orgAppService = inject(OrganizationApplicationService);

  private id = this.activatedRoute.snapshot.paramMap.get('id') as string;

  ngOnInit() {
    this.layoutService.setPageTitle('Vinculación de Políticas');
    this.layoutService.breadcrumbs.set([
      { label: 'Usuarios', url: '/users' },
      { label: 'Politicas', url: `users/user-policy/${this.id}` },
    ]);
    this.getUserById();
  }

  goBackToList() {
    this.router.navigate(['/users']);
  }

  async getUserById() {
    try {
      this.isLoadingUser.set(true);
      if (!this.id) return;
      const user = await firstValueFrom(this.userService.findOne(this.id));
      this.userData.set(user);
      this.personData.set(user.personnel);
      await this.buildUserApps(user);
    } catch (_error) {
      this.notification.error('Error al consultar la información del usuario');
    } finally {
      this.isLoadingUser.set(false);
    }
  }

  async buildUserApps(user: UserAdapted) {
    const userApplications = user.userApplications;
    if (!userApplications?.length) return;

    const orgIds = [
      ...new Set(
        userApplications
          .map((a) => String(a.organizationId))
          .filter((v) => v && v !== 'undefined'),
      ),
    ];

    const allOrgApps: OrganizationApplicationAdapted[] = [];
    for (const orgId of orgIds) {
      try {
        const res = await firstValueFrom(
          this.orgAppService.findAllByOrganizationId(orgId),
        );
        allOrgApps.push(...res.data);
      } catch (_) {
        // silent
      }
    }
    const entries: UserAppEntry[] = userApplications
      .map((ua) => {
        const orgApp = allOrgApps.find(
          (o) => o.organizationApplicationId === ua.organizationApplicationId,
        );
        return {
          userApplicationId: ua.userApplicationId ?? '',
          organizationApplicationId: ua.organizationApplicationId ?? 0,
          organizationId: String(ua.organizationId ?? ''),
          organizationName: orgApp?.organizationName ?? '',
          applicationName: orgApp?.application?.name ?? '',
          roleName: ua.roleName ?? '',
        };
      })
      .filter((e) => !!e.userApplicationId && !!e.organizationName);

    this.userApps.set(entries);
  }

  getAvailablePolicies(userApplicationId: string): PolicyAdapted[] {
    return this.appAvailablePoliciesMap().get(userApplicationId) ?? [];
  }

  async toggleApp(userApplicationId: string) {
    if (this.expandedAppId() === userApplicationId) {
      this.expandedAppId.set(null);
      return;
    }
    this.expandedAppId.set(userApplicationId);
    if (!this.loadedAppIds().has(userApplicationId)) {
      await this.loadAppPolicies(userApplicationId);
    }
  }

  async loadAppPolicies(userApplicationId: string) {
    this.loadingAppId.set(userApplicationId);
    try {
      const app = this.userApps().find(
        (a) => a.userApplicationId === userApplicationId,
      );
      const orgId = app?.organizationId ?? '';

      let orgPolicies: PolicyAdapted[];
      if (this.orgPoliciesCache.has(orgId)) {
        orgPolicies = this.orgPoliciesCache.get(orgId)!;
      } else {
        const res = await firstValueFrom(
          this.policyService.findAllByOrganization(orgId),
        );
        orgPolicies = res.data.map((policy) => {
          const conditionFormatted = (policy.conditions || []).reduce(
            (acc, c) => {
              acc[c.conditionKey] = c.conditionValue;
              return acc;
            },
            {} as Record<string, string>,
          );
          return { ...policy, conditionFormatted };
        });
        this.orgPoliciesCache.set(orgId, orgPolicies);
      }

      const { data } = await firstValueFrom(
        this.userAppPolicyService.findAllByUserApp(userApplicationId),
      );
      const policyMap = new Map(
        data
          .filter((a) => a.isActive)
          .map(
            (a) => [a.policyId, a.userApplicationPolicyId] as [string, string],
          ),
      );

      this.appAvailablePoliciesMap.update((m) =>
        new Map(m).set(userApplicationId, orgPolicies),
      );
      this.appPoliciesMap.update((m) =>
        new Map(m).set(userApplicationId, policyMap),
      );
    } catch {
      this.appPoliciesMap.update((m) =>
        new Map(m).set(userApplicationId, new Map()),
      );
      this.appAvailablePoliciesMap.update((m) =>
        new Map(m).set(userApplicationId, []),
      );
    } finally {
      this.loadedAppIds.update((s) => new Set(s).add(userApplicationId));
      this.loadingAppId.set(null);
    }
  }

  isPolicyLinked(userApplicationId: string, policyId: string): boolean {
    return this.appPoliciesMap().get(userApplicationId)?.has(policyId) ?? false;
  }

  async associatePolicy(userApplicationId: string, policyId: string) {
    try {
      this.pendingPolicyId.set(policyId);
      await firstValueFrom(
        this.userAppPolicyService.create({
          userApplicationId,
          policyId,
          isActive: true,
        }),
      );
      await this.silentReloadAppPolicies(userApplicationId);
      this.notification.success('Política asociada correctamente.');
    } catch {
      this.notification.error('Error al asociar la política.');
    } finally {
      this.pendingPolicyId.set(null);
    }
  }

  async deactivatePolicy(userApplicationId: string, policyId: string) {
    const userApplicationPolicyId = this.appPoliciesMap()
      .get(userApplicationId)
      ?.get(policyId);
    if (!userApplicationPolicyId) return;
    try {
      this.pendingPolicyId.set(policyId);
      await firstValueFrom(
        this.userAppPolicyService.deactivate(userApplicationPolicyId),
      );
      this.appPoliciesMap.update((m) => {
        const next = new Map(m);
        const appMap = new Map(next.get(userApplicationId));
        appMap.delete(policyId);
        next.set(userApplicationId, appMap);
        return next;
      });
      this.notification.success('Política desvinculada correctamente.');
    } catch {
      this.notification.error('Error al desvincular la política.');
    } finally {
      this.pendingPolicyId.set(null);
    }
  }

  private async silentReloadAppPolicies(userApplicationId: string) {
    try {
      const { data } = await firstValueFrom(
        this.userAppPolicyService.findAllByUserApp(userApplicationId),
      );
      const map = new Map(
        data
          .filter((a) => a.isActive)
          .map(
            (a) => [a.policyId, a.userApplicationPolicyId] as [string, string],
          ),
      );
      this.appPoliciesMap.update((m) => new Map(m).set(userApplicationId, map));
    } catch {
      // silent — optimistic state remains
    }
  }

  showDeleteConfirmationPolicy(userApplicationId: string, policyId: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar Política',
      name: '',
      paragraph: `Está seguro(a) que desea desvincular esta política al usuario`,
      confirmButton: 'Desvincular',
      onConfirm: () => {
        this.deactivatePolicy(userApplicationId, policyId);
      },
    });
  }

  showAssociateConfirmationPolicy(userApplicationId: string, policyId: string) {
    this.confirmationModal.openModal({
      isDelete: false,
      title: 'Vincular Política',
      name: '',
      paragraph: `Está seguro(a) que desea vincular esta política al usuario`,
      confirmButton: 'Vincular',
      onConfirm: () => {
        this.associatePolicy(userApplicationId, policyId);
      },
    });
  }
}
