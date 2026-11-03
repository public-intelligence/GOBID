import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import {
  NotificationService,
  OrganizationApplicationService,
} from '../../../services';
import { ActivatedRoute } from '@angular/router';
import { ColumnData, RolAppFilter, RoleAdapted } from '../../../models';
import { firstValueFrom } from 'rxjs';
import { faTrash } from '@fortawesome/free-solid-svg-icons';
import { Maintenance } from '../../../components/ui/layouts/maintenance/maintenance.layout';
import { RoleService } from '../../../services/role/role.service';
import { RoleModal } from '../../../components/ui/modals/role-modal/role-modal';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import { faEdit } from '@fortawesome/free-regular-svg-icons';

@Component({
  selector: 'app-role',
  imports: [Maintenance, RoleModal, ConfirmationModalComponent],
  templateUrl: './role.html',
})
export class Role implements OnInit {
  @ViewChild(RoleModal) roleModal!: RoleModal;
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  headers = headers;
  title = 'Listado de Roles';

  private roleService = inject(RoleService);
  private orgAppService = inject(OrganizationApplicationService);
  private notification = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private layoutService = inject(LayoutService);

  private orgAppId = this.route.snapshot.params['orgAppId'];

  private roles = signal<RoleAdapted[]>([] as RoleAdapted[]);
  private applicationId = signal<number | null>(null);
  protected applicationName = signal<string>('');
  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);
  protected rolAppFilter = signal<RolAppFilter>({
    pageNumber: 1,
    pageSize: 300,
  });

  ngOnInit(): void {
    this.layoutService.setPageTitle('Roles');
    this.layoutService.breadcrumbs.set([
      { label: 'Aplicaciones', url: '/role-application' },
      { label: 'roles', url: `/role-application/${this.orgAppId}/roles` },
    ]);
    this.fetchData();
  }

  /////////////// Services /////////////////

  fetchData = async () => {
    this.isLoading.set(true);
    this.tableData.set([]);
    await this.getApplicationByOrgAppId();
    await this.getRoles();
    this.isLoading.set(false);
  };

  async getApplicationByOrgAppId() {
    try {
      const response = await firstValueFrom(
        this.orgAppService.findOne(this.orgAppId),
      );
      this.applicationId.set(response.applicationId);
      this.applicationName.set(response.application?.name);
    } catch (_error) {
      this.notification.error('Error al obtener la aplicación');
    }
  }

  async getRoles() {
    try {
      const response = await firstValueFrom(
        this.roleService.findAll({
          OrganizationApplicationId: this.orgAppId,
          ...this.rolAppFilter(),
        }),
      );
      this.roles.set(response.data);
      this.tableData.set(this.formattedData(response.data));
    } catch (_error) {
      this.notification.error('Error al consultar los roles');
    }
  }

  async deleteRole(id: number) {
    try {
      await firstValueFrom(this.roleService.delete(id));
      this.notification.success('Rol eliminado correctamente');
      this.fetchData();
    } catch (_error) {
      this.notification.error('Error al eliminar el rol');
    }
  }

  ////////////// Helpers /////////////////////

  createRole = () => {
    this.roleModal.openModal({
      applicationId: this.applicationId(),
      orgAppId: this.orgAppId,
    });
  };

  openModal(roleId: number) {
    this.roleModal.openModal({
      roleId,
      applicationId: this.applicationId(),
      orgAppId: this.orgAppId,
    });
  }

  showDeleteConfirmation(id: number, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar rol',
      name,
      paragraph: `Está seguro(a) que desea eliminar el rol`,
      warning: 'Nadie podrá acceder a este rol.',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.deleteRole(id);
      },
    });
  }

  formattedData(data: RoleAdapted[]): ColumnData[] {
    return data.map((item) => ({
      id: String(item.id),
      name: item.name,
      showActions: false,
      actionsOptions: [
        {
          name: 'Editar permisos',
          icon: faEdit,
          function: () => {
            this.openModal(item.id);
          },
        },
        {
          name: 'Eliminar Rol',
          icon: faTrash,
          color: 'red',
          function: () => {
            this.showDeleteConfirmation(item.id, item.name);
          },
        },
      ],
      columns: [
        { value: item.name, dataType: 'string' },
        {
          value: item.userCount?.toString() ?? '0',
          dataType: 'number',
          justify: 'center',
        },
        {
          value: item.claimCount?.toString() ?? '0',
          dataType: 'number',
          justify: 'center',
        },
      ],
    }));
  }

  private updateFilters(partialFilters: Partial<RolAppFilter>) {
    this.rolAppFilter.update((filters) => ({
      ...filters,
      ...partialFilters,
    }));
    this.getRoles();
  }

  onSearchChange(search: string) {
    this.updateFilters({ pageNumber: 1, search });
  }
}
