import { Component, computed, inject, Input, signal, ViewChild } from '@angular/core';
import { ModalComponent, InputComponent, CustomSelectSearch } from '../../../shared';
import { RoleClaimService } from '../../../../services/role-claim/role-claim.service';
import { firstValueFrom } from 'rxjs';
import {
  ClaimAdapted,
  CreateRoleClaim,
  Role,
  RoleClaimAdapted,
  Select,
} from '../../../../models';
import { ClaimService, NotificationService, PositionService } from '../../../../services';
import { faEdit, faXmark } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgClass } from '@angular/common';
import { RoleService } from '../../../../services/role/role.service';
import { faCircleCheck } from '@fortawesome/free-regular-svg-icons';

@Component({
  selector: 'app-role-modal',
  imports: [
    ModalComponent,
    FontAwesomeModule,
    InputComponent,
    ReactiveFormsModule,
    NgClass,
    CustomSelectSearch,
  ],
  templateUrl: './role-modal.html',
})
export class RoleModal {
  @ViewChild(ModalComponent) modalComponent!: ModalComponent;

  @Input() onRefresh!: () => void;

  title = 'Datos del Rol';
  faEdit = faEdit;
  faX = faXmark;
  faCircleCheck = faCircleCheck;

  private roleClaimService = inject(RoleClaimService);
  private claimService = inject(ClaimService);
  private roleService = inject(RoleService);
  private positionService = inject(PositionService);
  protected notificationService = inject(NotificationService);

  protected roleId = signal<number | null>(null);
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
  private applicationId = signal<number | null>(null);
  protected orgAppId = signal<number | null>(null);
  protected roleClaim = signal<RoleClaimAdapted[] | null>(null);
  protected claims = signal<ClaimAdapted[] | null>(null);
  protected isLoading = signal<boolean>(false);
  protected tipo = signal<'rol' | 'cargo'>('rol');

  fb = new FormBuilder();
  roleForm = this.fb.group({
    id: [null],
    name: ['', Validators.required],
    claimIds: [this.fb.array([], Validators.required)],
    positionId: [''],
  });

  /**
   * Cierra el modal de roles
   */
  closeModal = () => {
    this.roleId.set(null);
    this.applicationId.set(null);
    this.roleClaim.set(null);
    this.claims.set(null);
    this.tipo.set('rol');
    this.currentPositionOption.set(null);
    this.roleForm.reset();
    this.modalComponent.toggleModal(false);
  };

  /**
   * Abre el modal de roles
   * @param title Título del modal
   * @param paragraph Texto del modal
   * @param name Nombre del elemento a eliminar
   * @param confirmButton Texto del botón de confirmación
   * @param warning Texto de advertencia
   * @param onConfirm Función a ejecutar al confirmar
   * @param isDelete Indica si es una acción de eliminación
   */
  openModal({
    roleId,
    applicationId,
    orgAppId,
  }: {
    roleId?: number;
    applicationId: number | null;
    orgAppId: number | null;
  }) {
    this.modalComponent.toggleModal(true);
    this.roleId.set(roleId ?? null);
    this.applicationId.set(applicationId);
    this.orgAppId.set(orgAppId);
    this.fetchData();
  }

  //////////////// Getters and Setters //////////////////

  get roleClaimsSelected(): number[] {
    const value = this.roleForm.controls['claimIds'].value;
    return Array.isArray(value) ? (value as number[]) : [];
  }

  set roleClaimsSelected(claimIds: number[]) {
    this.roleForm.controls['claimIds'].setValue(claimIds);
  }

  get roleName() {
    return this.roleForm.controls['name'].value ?? '';
  }

  set roleName(name: string) {
    this.roleForm.controls['name'].setValue(name);
  }

  get positionId() {
    return this.roleForm.controls['positionId'].value ?? '';
  }

  set positionId(positionId: string) {
    this.roleForm.controls['positionId'].setValue(positionId);
  }

  //////////////// Services //////////////////

  async fetchData() {
    this.isLoading.set(true);
    await Promise.all([
      this.getRoleClaims(),
      this.getClaims(),
      this.getRoleName(),
      this.getAllPositions(),
    ]);
    this.isLoading.set(false);
  }
  /**
   * Obtiene los permisos del rol
   */
  async getRoleClaims() {
    try {
      if (!this.roleId()) return;
      const response = await firstValueFrom(
        this.roleClaimService.findAll({
          roleId: this.roleId(),
          pageNumber: 1,
          pageSize: 1000,
        }),
      );
      this.roleClaim.set(response.data);
      const claimIds = response.data.map((claim) => claim.claimId);
      this.roleClaimsSelected = claimIds;
    } catch (_error) {
      this.notificationService.error('Error al obtener los permisos del rol');
    }
  }

  async getAllPositions(q = '') {
    try {
      const posResponse = await firstValueFrom(this.positionService.search(q, { pageSize: 200 }));
      const raw = (posResponse?.data ?? posResponse ?? []) as { id: string; name: string }[];
      this.positionsOptions.set(raw.map((p) => ({ value: p.id, label: p.name })));
    } catch (_error) {
      this.notificationService.error('Error al obtener los cargos');
    }
  }

  protected onSearchPositionsFilter(value: string | number): void {
    this.getAllPositions(String(value));
  }

  async onPositionSelected(value: string | number) {
    const position = this.positionsOptions().find((p) => p.value == value);
    if (position) {
      this.positionId = position.value.toString();
      this.roleName = position.label;
    }
  }

  async getRoleName() {
    try {
      if (!this.roleId()) return;
      const response = await firstValueFrom(
        this.roleService.findOne(this.roleId() ?? 0),
      );
      this.roleName = response.name;
      this.positionId = response.positionId ?? '';
      this.tipo.set(response.positionId ? 'cargo' : 'rol');
      if (response.positionId) {
        this.currentPositionOption.set({ value: response.positionId, label: response.name });
      }
    } catch (_error) {
      this.notificationService.error('Error al obtener el nombre del rol');
    }
  }

  /**
   * Obtiene los permisos de la aplicación
   */
  async getClaims() {
    try {
      if (!this.applicationId()) return;
      const response = await firstValueFrom(
        this.claimService.findAll({
          applicationId: this.applicationId(),
          pageNumber: 1,
          pageSize: 1000,
        }),
      );
      this.claims.set(response.data);
    } catch (_error) {
      this.notificationService.error('Error al obtener los permisos del rol');
    }
  }

  /////////////////// Helpers ///////////////////////////

  isClaimSelected(claimId: number): boolean {
    return this.roleClaimsSelected.includes(claimId);
  }

  toggleClaimSelection(claimId: number): void {
    const selected = this.roleClaimsSelected;
    if (selected.includes(claimId)) {
      this.roleClaimsSelected = selected.filter((id) => id !== claimId);
    } else {
      this.roleClaimsSelected = [...selected, claimId];
    }
  }

  ///////////////// Handlers //////////////////

  async onSubmit() {
    try {
      if (this.roleId()) {
        await this.onUpdateRole(this.roleId() ?? 0);
        await this.onAssignPermissionsToRole(this.roleId() ?? 0);
      } else {
        const roleId = await this.onCreateRole();
        await this.onAssignPermissionsToRole(roleId);
      }
      this.closeModal();
      this.onRefresh();
    } catch (error) {
      this.notificationService.error((error as Error).message);
    }
  }

  async onCreateRole() {
    try {
      const response = await firstValueFrom(
        this.roleService.create({
          roleId: 0,
          roleName: this.roleName,
          roleDescription: '',
          setAutomaticClaims: false,
          organizationApplicationId: this.orgAppId() ?? 0,
          isSystemRole: false,
          isActive: true,
          positionId: this.positionId,
        }),
      );

      const roleId = (response as { data: Role }).data.roleId;
      this.notificationService.success('El rol fue creado con existosamente');

      return roleId;
    } catch {
      throw new Error('Error al crear el rol');
    }
  }

  async onUpdateRole(roleId: number) {
    try {
      const response = await firstValueFrom(
        this.roleService.create({
          roleId,
          roleName: this.roleName,
          roleDescription: '',
          setAutomaticClaims: false,
          organizationApplicationId: this.orgAppId() ?? 0,
          isSystemRole: false,
          isActive: true,
          positionId: this.positionId,
        }),
      );
      this.notificationService.success(
        'El rol fue actualizado con existosamente',
      );

      return (response as { data: Role }).data.roleId;
    } catch {
      throw new Error('Error al crear el rol');
    }
  }

  async onAssignPermissionsToRole(roleId: number) {
    try {
      const currentRoleClaims = this.roleClaim() ?? [];
      const currentClaimIds = currentRoleClaims.map((claim) => claim.claimId);
      const newClaimIds = this.roleClaimsSelected;

      const claimsToRemove = currentClaimIds.filter(
        (claimId) => !newClaimIds.includes(claimId),
      );

      const claimsToAdd = newClaimIds.filter(
        (claimId) => !currentClaimIds.includes(claimId),
      );

      if (claimsToRemove.length > 0) {
        const deletePromises = claimsToRemove.map((claimId) => {
          const roleClaimToDelete = currentRoleClaims.find(
            (rc) => rc.claimId === claimId,
          );
          if (roleClaimToDelete) {
            return firstValueFrom(
              this.roleClaimService.delete(roleClaimToDelete.id),
            );
          }
          return Promise.resolve();
        });
        await Promise.all(deletePromises);
      }

      if (claimsToAdd.length > 0) {
        const formattedData: CreateRoleClaim[] = claimsToAdd.map((claimId) => ({
          roleId: roleId,
          claimId: claimId,
          isActive: true,
        }));
        await firstValueFrom(this.roleClaimService.create(formattedData));
      }
    } catch {
      throw new Error('Error al asignarle los permisos al rol');
    }
  }
}
