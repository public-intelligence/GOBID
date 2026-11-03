import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import { filters, headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import {
  ColumnData,
  DisplayData,
  OrganizationCreate,
  OrganizationFilter,
} from '../../models';
import { OrganizationService } from '../../services/organization/organization.service';
import { firstValueFrom } from 'rxjs';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faLink } from '@fortawesome/free-solid-svg-icons';
import { Router } from '@angular/router';
import { NotificationService } from '../../services';
import { OrganizationAdapted } from '../../models';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal';

@Component({
  selector: 'app-organization',
  imports: [Maintenance, FontAwesomeModule, ConfirmationModalComponent],
  templateUrl: './organization.html',
})
export class OrganizationComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;
  private router = inject(Router);
  private notification = inject(NotificationService);
  private organizationService = inject(OrganizationService);
  private organization = signal<OrganizationAdapted[]>([]);

  protected headers = headers;
  protected filters = filters;
  protected title = 'Listado de Organizaciones';

  private layoutService = inject(LayoutService);
  protected faLink = faLink;
  protected editPath = (id: number) => `/organizations/edit/${id}`;
  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal(false);
  protected organizationFilter = signal<OrganizationFilter>({
    IsActive: 'true',
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);

  ngOnInit() {
    this.layoutService.setPageTitle('Organizaciones');
    this.layoutService.breadcrumbs.set([
      { label: 'Organizaciones', url: '/organizations' },
    ]);
    this.getOrganizations();
  }

  /**
   *  Recupera todas las organizaciones.
   */
  getOrganizations = async () => {
    this.isLoading.set(true);
    try {
      const response = await firstValueFrom(
        this.organizationService.findAll(this.organizationFilter()),
      );
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      this.tableData.set(this.formattedData(response.data));
    } catch (_error) {
      this.notification.error('Error al consultar las organizaciones');
    } finally {
      this.isLoading.set(false);
    }
  };

  formattedDisplayData(data: OrganizationAdapted): DisplayData[] {
    return [
      {
        columnName: 'Nombre de la Organización',
        value: data.name,
        type: 'string',
      },
      {
        columnName: 'Descripción',
        value: data.description,
        type: 'string',
      },
      {
        columnName: 'Página Web',
        value: data.website,
        type: 'link',
      },
      {
        columnName: 'Correo Electrónico',
        value: data.email,
        type: 'string',
      },
      {
        columnName: 'Teléfono',
        value: data.phone,
        type: 'string',
      },
      {
        columnName: 'estado',
        value: data.isActive ? 'Activo' : 'Inactivo',
        type: 'boolean',
      },
      {
        columnName: 'fecha de creación',
        value: data.createdAt ?? '',
        type: 'date',
      },
    ];
  }

  /**
   * Formatea para la tabla.
   */
  formattedData(data: OrganizationAdapted[]): ColumnData[] {
    return data.map((item) => {
      const currentId = item.id.toString();
      const isActive = item.isActive;
      const statusText = isActive ? 'Activo' : 'Inactivo';

      return {
        id: currentId,
        name: item.name,
        editPath: this.editPath(item.id),
        showActions: false,
        displayData: this.formattedDisplayData(item),
        actionsOptions: [
          {
            name: 'Editar',
            function: () => this.onEditOrganization(currentId),
          },
          {
            name: !isActive ? 'Activar' : 'Desactivar',
            color: !isActive ? 'green' : 'orange',
            function: () => {
              this.showActivateOrDisableConfirmation(
                item,
                !isActive ? 'activate' : 'deactivate',
              );
            },
          },
          {
            name: 'Eliminar',
            color: 'red',
            function: () => {
              this.showDeleteConfirmation(currentId, item.name);
            },
          },
        ],
        columns: [
          {
            value: item.legalEntity?.legalName ?? item?.name,
            dataType: 'string',
          },
          {
            value: item.legalEntity?.identificationNumber ?? '',
            dataType: 'string',
          },
          { value: item.acronym, dataType: 'string' },
          { value: item.legalEntity?.status ?? '', dataType: 'string' },
          { value: statusText, dataType: 'string' },
        ],
      };
    });
  }

  onEditOrganization = (id: string) => {
    this.router.navigate([`/organizations/edit/${id}`]);
  };

  /**
   * Elimina una organizació.
   * @param id Identificador de la organización a desactivar/activar.
   */
  onDeleteOrganization = async (id: string) => {
    try {
      const response = await firstValueFrom(
        this.organizationService.findOne(id),
      );
      await firstValueFrom(
        this.organizationService.delete(response.id.toString()),
      );
      this.getOrganizations();
    } catch (_error) {
      this.notification.error('Error al desactivar la organización');
    }
  };

  showActivateOrDisableConfirmation(
    item: OrganizationAdapted,
    action: 'activate' | 'deactivate',
  ) {
    const isActivate = action === 'activate';
    const titleActionName = isActivate ? 'Activar' : 'Desactivar';
    const confirmationActionName = isActivate ? 'activar' : 'desactivar';

    this.confirmationModal.openModal({
      title: `${titleActionName} organización`,
      name: item.name,
      paragraph: `¿Está seguro(a) que desea ${confirmationActionName} la organización?`,
      confirmButton: titleActionName,
      onConfirm: () => {
        this.onActiveOrganization(
          {
            legalEntityId: item.legalEntityId,
            organizationId: item.id,
            organizationName: item.name,
            acronym: item.acronym,
            organizationDescription: item.description,
            slogan: item.slogan,
            website: item.website,
            email: item.email,
            organizationAddress: item.organizationAddress,
            phone: item.phone,
            isActive: isActivate,
          },
          isActivate,
        );
      },
    });
  }

  onActiveOrganization = async (data: OrganizationCreate, status: boolean) => {
    try {
      const updatedOrganization: OrganizationCreate = {
        legalEntityId: data.legalEntityId,
        organizationId: data.organizationId,
        organizationName: data.organizationName,
        acronym: data.acronym,
        organizationDescription: data.organizationDescription,
        slogan: data.slogan,
        website: data.website,
        email: data.email,
        organizationAddress: data.organizationAddress,
        phone: data.phone,
        isActive: status,
      };
      await firstValueFrom(
        this.organizationService.upsert(updatedOrganization),
      );
      this.getOrganizations();
      this.notification.success(
        !status
          ? 'Organización desactivada correctamente'
          : 'Organización activada correctamente',
      );
    } catch (_error) {
      this.notification.error('Error al realizar la operación');
    }
  };

  /**
   * Navega a la página de creación de una nueva organización.
   */
  onNavigateTo = () => {
    this.router.navigate(['/organizations/create']);
  };

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar Organización',
      name,
      paragraph: `Está seguro(a) que desea eliminar la organización`,
      warning:
        'Al desactivar esta organización, se bloqueará el acceso de autenticación para todos los usuarios vinculados a ella.',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeleteOrganization(id);
      },
    });
  }

  private updateFilters(partialFilters: Partial<OrganizationFilter>) {
    this.organizationFilter.update((filters) => ({
      ...filters,
      ...partialFilters,
    }));
    this.getOrganizations();
  }

  onSearchChange(search: string) {
    this.updateFilters({ pageNumber: 1, search });
  }

  onFiltersChange(filters: OrganizationFilter) {
    this.updateFilters({ pageNumber: 1, ...filters });
  }

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
