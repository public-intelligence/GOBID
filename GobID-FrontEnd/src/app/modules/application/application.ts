import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { breadcrumbs, filters, headers } from './config';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import {
  ApplicationService,
  NotificationService,
  OrganizationApplicationService,
  UtilsService,
} from '../../services';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationAdapted,
  ApplicationCreate,
  ApplicationFilter,
  ColumnData,
  DisplayData,
} from '../../models';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal';
import { Router } from '@angular/router';
import { TokenDecoder } from '../../services/token-decoder/token-decoder';
import { environment } from '../../../environments/environment';
import { LayoutService } from '@public-intelligence/ui-core';

@Component({
  selector: 'app-application',
  imports: [Maintenance, ConfirmationModalComponent],
  templateUrl: './application.html',
})
export class Application implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  title = 'Listado de Aplicaciones';
  createPath = '/applications/create';
  editPath = (id: string) => `/applications/edit/${id}`;

  private utilsService = inject(UtilsService);
  private applicationService = inject(ApplicationService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private tokenDecoder = inject(TokenDecoder);
  private organizationApplication = inject(OrganizationApplicationService);
  private layoutService = inject(LayoutService);

  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);
  protected filters = filters;
  protected applicationFilter = signal<ApplicationFilter>({
    IsActive: 'true',
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);
  protected permissions = this.tokenDecoder.getPermissions();
  protected isLocalAdmin = this.permissions.includes('admin_local');
  headers = headers(this.isLocalAdmin);

  ngOnInit(): void {
    this.layoutService.setPageTitle('Aplicaciones');
    this.layoutService.breadcrumbs.set([
      { label: 'Aplicaciones', url: '/apps' },
    ]);
    if (this.isLocalAdmin) {
      this.getApplicationsByOrganization();
      return;
    }
    this.getApplications();
  }

  /////////////// Services /////////////////

  async getApplicationsByOrganization() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.organizationApplication.findAllByToken(this.applicationFilter()),
      );
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      const applications = response.data.map((item) => {
        return {
          ...item.application,
          organizationApplicationId: item.organizationApplicationId,
        };
      });
      this.tableData.set(this.formattedData(applications));
    } catch (_error) {
      this.notification.error(
        'Error al consultar las aplicaciones por organización',
      );
    } finally {
      this.isLoading.set(false);
    }
  }

  async getApplications() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.applicationService.findAll(this.applicationFilter()),
      );
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      this.tableData.set(this.formattedData(response.data));
    } catch (_error) {
      this.notification.error('Error al consultar las aplicaciones');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onDeleteApplication(id: string) {
    try {
      await firstValueFrom(this.applicationService.delete(id));
      this.notification.success('Aplicación eliminada correctamente');
      this.getApplications();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al eliminar la aplicación');
    }
  }

  async onUpdateStatusApp(data: ApplicationCreate) {
    try {
      await firstValueFrom(this.applicationService.update(data));
      this.notification.success('Aplicación actualizada correctamente');
      this.getApplications();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al actualizar la aplicación');
    }
  }

  async onDuplicateApplication(id: string) {
    try {
      await firstValueFrom(this.applicationService.duplicate(id));
      this.notification.success('Aplicación duplicada correctamente');
      this.getApplications();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al duplicar la aplicación');
    }
  }

  /////////////// Helpers /////////////////

  goToEdit(id: string) {
    this.router.navigate([this.editPath(id)]);
  }

  formattedDisplayData(
    data: ApplicationAdapted,
    statusText: string,
  ): DisplayData[] {
    return [
      {
        columnName: 'ID',
        value: String(data.id),
        type: 'string',
      },
      {
        columnName: 'código',
        value: data.code,
        type: 'string',
      },
      {
        columnName: 'nombre',
        value: data.name,
        type: 'string',
      },
      {
        columnName: 'descripción',
        value: data.description,
        type: 'string',
      },
      {
        columnName: 'redirect URL',
        value: data.redirectUrl,
        type: 'link',
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

  formatLink(id: string) {
    return `${environment.appUrl}/app/${id}/login`;
  }

  formattedData(data: ApplicationAdapted[]): ColumnData[] {
    return data.map(
      (item: ApplicationAdapted & { organizationApplicationId?: string }) => {
        const currentId = item.id.toString();
        const statusText = item.isActive ? 'Activo' : 'Inactivo';
        return {
          id: currentId,
          name: item.name,
          showActions: false,
          displayData: this.formattedDisplayData(item, statusText),
          editPath: this.editPath(currentId),
          actionsOptions: !this.isLocalAdmin
            ? [
                {
                  name: 'Editar',
                  function: () => {
                    this.goToEdit(currentId);
                  },
                },
                {
                  name: 'Duplicar',
                  function: () => {
                    this.showDuplicateConfirmation(currentId, item.name);
                  },
                },
                {
                  name: 'Credenciales M2M',
                  function: () => {
                    this.router.navigate([
                      `/applications/${currentId}/credentials`,
                    ]);
                  },
                },
                item.isActive
                  ? {
                      name: 'Desactivar',
                      color: 'orange',
                      function: () => {
                        this.showActivateConfirmation(item, 'deactivate');
                      },
                    }
                  : {
                      name: 'Activar',
                      color: 'green',
                      function: () => {
                        this.showActivateConfirmation(item, 'activate');
                      },
                    },
                {
                  name: 'Eliminar',
                  color: 'red',
                  function: () => {
                    this.showDeleteConfirmation(currentId, item.name);
                  },
                },
              ]
            : [],
          columns: [
            { value: String(item.id), dataType: 'string' as const },
            { value: item.code, dataType: 'string' as const },
            { value: item.name, dataType: 'string' as const },
            { value: item.description, dataType: 'string' as const },
            ...(this.isLocalAdmin
              ? [
                  {
                    value: this.formatLink(item.id.toString() ?? ''),
                    dataType: 'link' as const,
                  },
                ]
              : []),
            {
              value: this.utilsService.formatDate(item.createdAt),
              dataType: 'string' as const,
            },
            {
              value: item.isPublic ? 'Si' : 'No',
              dataType: 'string' as const,
              justify: 'center' as const,
            },
            {
              value: statusText,
              dataType: 'string' as const,
            },
          ],
        };
      },
    );
  }

  showActivateConfirmation(
    item: ApplicationAdapted,
    action: 'activate' | 'deactivate',
  ) {
    const isActivate = action === 'activate';
    const confirmationActionName = isActivate ? 'activar' : 'desactivar';
    const titleActionName = isActivate ? 'Activar' : 'Desactivar';

    this.confirmationModal.openModal({
      title: `${titleActionName} aplicación`,
      name: item.name,
      paragraph: `¿Está seguro(a) que desea ${confirmationActionName} la aplicación?`,
      confirmButton: titleActionName,
      onConfirm: () => {
        this.onUpdateStatusApp({
          applicationId: item.id,
          applicationName: item.name,
          applicationDescription: item.description,
          redirectUrl: item.redirectUrl,
          code: item.code,
          isActive: isActivate,
        });
      },
    });
  }

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar aplicación',
      name,
      paragraph: `Está seguro(a) que desea eliminar la aplicación`,
      warning: 'Nadie podrá acceder a esta aplicación.',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeleteApplication(id);
      },
    });
  }

  showDuplicateConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      title: 'Duplicar aplicación',
      name,
      paragraph: `Está seguro(a) que desea duplicar la aplicación`,
      confirmButton: 'Duplicar',
      onConfirm: () => {
        this.onDuplicateApplication(id);
      },
    });
  }

  private updateFilters(partialFilters: Partial<ApplicationFilter>) {
    this.applicationFilter.update((filters) => ({
      ...filters,
      ...partialFilters,
    }));
    if (this.isLocalAdmin) {
      this.getApplicationsByOrganization();
      return;
    }
    this.getApplications();
  }

  onSearchChange(search: string) {
    this.updateFilters({ pageNumber: 1, search });
  }

  onFiltersChange(filters: ApplicationFilter) {
    this.updateFilters({ pageNumber: 1, ...filters });
  }

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
