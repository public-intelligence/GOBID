import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { filters, headers } from './config';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import { NotificationService, UserService } from '../../services';
import { LayoutService } from '@public-intelligence/ui-core';
import { firstValueFrom } from 'rxjs';
import {
  ColumnData,
  DisplayData,
  UserAdapted,
  UserFilter,
  UserStatus,
} from '../../models';
import { Router } from '@angular/router';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal';

@Component({
  selector: 'app-user-list',
  imports: [Maintenance, ConfirmationModalComponent],
  templateUrl: './user.html',
})
export class User implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  headers = headers;
  filters = filters;
  title = 'Listado de Usuarios';

  private layoutService = inject(LayoutService);
  createPath = '/users/create';
  editPath = (id: string) => `/users/edit/${id}`;
  linkPath = (id: string) => `/users/link/${id}`;

  private userService = inject(UserService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  protected userFilters = signal<UserFilter>({
    IsActive: 'true',
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);

  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);

  ngOnInit(): void {
    this.layoutService.setPageTitle('Usuarios');
    this.layoutService.breadcrumbs.set([{ label: 'Usuarios', url: '/users' }]);
    this.getUsers();
  }

  /////////////// Services /////////////////

  async getUsers() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.userService.findAll(this.userFilters()),
      );
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      this.tableData.set(this.formattedData(response.data));
    } catch (_error) {
      this.notification.error('Error al consultar los usuarios');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onActivateOrDisableUser(userId: string, isActive: boolean) {
    try {
      await firstValueFrom(this.userService.updateStatus({ userId, isActive }));
      this.notification.success(
        `Usuario ${isActive ? 'activado' : 'desactivado'} correctamente`,
      );
      this.getUsers();
    } catch (_error) {
      this.notification.error(
        `Error al ${isActive ? 'activar' : 'desactivar'} el usuario`,
      );
    }
  }

  async onDeleteUser(id: string) {
    try {
      await firstValueFrom(this.userService.delete(id));
      this.notification.success('Usuario eliminado correctamente');
      this.getUsers();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al eliminar el usuario');
    }
  }

  /////////////// Helpers /////////////////

  goToPath(path: string) {
    this.router.navigate([path]);
  }

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar usuario',
      name,
      paragraph: `Está seguro(a) que desea eliminar el usuario`,
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeleteUser(id);
      },
    });
  }

  showActivateOrDisableConfirmation(
    userId: string,
    name: string,
    action: 'activate' | 'deactivate',
  ) {
    const isActivate = action === 'activate';
    const confirmationActionName = isActivate ? 'activar' : 'desactivar';
    const titleActionName = isActivate ? 'Activar' : 'Desactivar';

    this.confirmationModal.openModal({
      title: `${titleActionName} usuario`,
      name,
      paragraph: `¿Está seguro(a) que desea ${confirmationActionName} el usuario?`,
      confirmButton: titleActionName,
      onConfirm: () => {
        this.onActivateOrDisableUser(userId, isActivate);
      },
    });
  }

  formattedDisplayData(
    data: UserAdapted,
    fullName: string,
    statusText: string,
  ): DisplayData[] {
    return [
      {
        columnName: 'Nombre Completo',
        value: fullName,
        type: 'string',
      },
      {
        columnName: 'Número de Identificación',
        value: data.personnel.identificationNumber,
        type: 'string',
      },
      {
        columnName: 'Cargo',
        value: data.position,
        type: 'string',
      },
      {
        columnName: 'Organización',
        value: data.organizationName,
        type: 'string',
      },
      {
        columnName: 'Email',
        value: data.email,
        type: 'string',
      },
      {
        columnName: '¿Es admin del servidor?',
        value: data.isServerAdmin ? 'Si' : 'No',
        type: 'string',
      },
      {
        columnName: 'estado',
        value: statusText,
        type: 'boolean',
      },
      {
        columnName: 'fecha de creación',
        value: data.createdAt ?? '',
        type: 'date',
      },
    ];
  }

  formattedData(data: UserAdapted[]): ColumnData[] {
    return data.map((item) => {
      const fullName = `${item.personnel.firstName} ${item.personnel.lastName}`;
      const statusText = item.isActive ? 'Activo' : 'Inactivo';

      return {
        id: String(item.id),
        name: item.personnelId,
        showActions: false,
        displayData: this.formattedDisplayData(item, fullName, statusText),
        editPath: this.editPath(item.id),
        actionsOptions: [
          {
            name: 'Vincular Apps',
            function: () => {
              this.goToPath(this.linkPath(item.id));
            },
          },
          {
            name: 'Vincular Políticas',
            function: () => {
              this.goToPath(`/users/user-policy/${item.id}`);
            },
          },
          {
            name: 'Editar',
            function: () => {
              this.goToPath(this.editPath(item.id));
            },
          },
          item.isActive
            ? {
                name: 'Desactivar',
                color: 'orange',
                function: () => {
                  this.showActivateOrDisableConfirmation(
                    String(item.userId ?? ''),
                    fullName,
                    'deactivate',
                  );
                },
              }
            : {
                name: 'Activar',
                color: 'green',
                function: () => {
                  this.showActivateOrDisableConfirmation(
                    String(item.userId ?? ''),
                    fullName,
                    'activate',
                  );
                },
              },
          {
            name: 'Eliminar',
            color: 'red',
            function: () => {
              this.showDeleteConfirmation(item.id, fullName);
            },
          },
        ],
        columns: [
          { value: item.personnel.firstName, dataType: 'string' },
          { value: item.personnel.lastName, dataType: 'string' },
          { value: item.email, dataType: 'string' },
          { value: item.organizationName, dataType: 'string' },
          { value: statusText, dataType: 'string' },
        ],
      };
    });
  }

  private updateFilters(partialFilters: Partial<UserFilter>) {
    this.userFilters.update((filters) => ({ ...filters, ...partialFilters }));
    this.getUsers();
  }

  onSearchChange(EmailLike: string) {
    this.updateFilters({ pageNumber: 1, EmailLike });
  }

  onFiltersChange(filters: UserFilter) {
    this.updateFilters({ pageNumber: 1, ...filters });
  }

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
