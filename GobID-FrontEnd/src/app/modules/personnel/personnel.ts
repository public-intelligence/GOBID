import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { filters, headers } from './config';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import { NotificationService, OrganizationService, TokenDecoder } from '../../services';
import { PersonnelService } from '../../services/personnel/personnel.service';
import { LayoutService } from '@public-intelligence/ui-core';
import { firstValueFrom } from 'rxjs';
import {
  ColumnData,
  DisplayData,
  OrganizationAdapted,
  PersonnelAdapted,
  PersonnelFilter,
  Select,
} from '../../models';
import { Router } from '@angular/router';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal';

@Component({
  selector: 'app-personnel-list',
  imports: [Maintenance, ConfirmationModalComponent],
  templateUrl: './personnel.html',
})
export class Personnel implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  headers = headers;
  filters = filters;
  title = 'Listado de Personal';

  private layoutService = inject(LayoutService);
  createPath = '/personnel/create';
  editPath = (id: string) => `/personnel/edit/${id}`;

  private personnelService = inject(PersonnelService);
  private organizationService = inject(OrganizationService);
  private notification = inject(NotificationService);
  private tokenDecoder = inject(TokenDecoder);
  private router = inject(Router);

  protected isSuperAdmin = this.tokenDecoder.getRole() === 'superadmin';

  protected organizationOptions = signal<Select[]>([]);
  protected selectedOrganizationId = signal<number | null>(null);

  protected personnelFilters = signal<PersonnelFilter>({
    isActive: 'true',
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);
  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);

  ngOnInit(): void {
    this.layoutService.setPageTitle('Personal');
    this.layoutService.breadcrumbs.set([
      { label: 'Personal', url: '/personnel' },
    ]);
    if (this.isSuperAdmin) {
      this.getOrganizations();
    } else {
      const orgId = Number(this.tokenDecoder.getOrganizationId());
      if (orgId) {
        this.selectedOrganizationId.set(orgId);
        this.getPersonnel(orgId);
      }
    }
  }

  async getOrganizations() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(this.organizationService.findAll());
      const options = response.data.map((org: OrganizationAdapted) => ({
        label: org.name,
        value: String(org.id),
      }));
      this.organizationOptions.set([
        { label: 'Seleccione una organización', value: 'default' },
        ...options,
      ]);
    } catch (_error) {
      this.notification.error('Error al consultar las organizaciones');
    } finally {
      this.isLoading.set(false);
    }
  }

  async getPersonnel(organizationId: number) {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.personnelService.findAllByOrganization(
          organizationId,
          this.personnelFilters(),
        ),
      );
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      this.tableData.set(this.formattedData(response.data));
    } catch (_error) {
      this.notification.error('Error al consultar el personal');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onDeletePersonnel(id: string) {
    try {
      await firstValueFrom(this.personnelService.delete(id));
      this.notification.success('Personal eliminado correctamente');
      const orgId = this.selectedOrganizationId();
      if (orgId) this.getPersonnel(orgId);
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al eliminar el personal');
    }
  }

  goToPath(path: string) {
    this.router.navigate([path]);
  }

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar personal',
      name,
      paragraph: 'Está seguro(a) que desea eliminar este personal',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeletePersonnel(id);
      },
    });
  }

  formattedDisplayData(data: PersonnelAdapted): DisplayData[] {
    return [
      { columnName: 'ID', value: data.personnelId, type: 'string' },
      {
        columnName: 'Nombre',
        value: data.person?.firstName + ' ' + data.person?.lastName,
        type: 'string',
      },
      {
        columnName: 'Posición',
        value: data.positionName,
        type: 'string',
      },
      {
        columnName: 'Localidad',
        value: data.localityName,
        type: 'string',
      },
      {
        columnName: 'Estado',
        value: data.isActive ? 'Activo' : 'Inactivo',
        type: 'boolean',
      },
    ];
  }

  formattedData(data: PersonnelAdapted[]): ColumnData[] {
    return data.map((item) => {
      const statusText = item.isActive ? 'Activo' : 'Inactivo';
      return {
        id: item.personnelId,
        name: `${item?.person?.firstName} ${item?.person?.lastName}`,
        showActions: false,
        displayData: this.formattedDisplayData(item),
        editPath: this.editPath(item.personnelId),
        actionsOptions: [
          {
            name: 'Editar',
            function: () => this.goToPath(this.editPath(item.personnelId)),
          },
          {
            name: 'Eliminar',
            color: 'red',
            function: () => this.showDeleteConfirmation(item.personnelId, `${item?.person?.firstName} ${item?.person?.lastName}`),
          },
        ],
        columns: [
          {
            value: item.person?.firstName + ' ' + item.person?.lastName,
            dataType: 'string',
          },
          {
            value: item.positionName,
            dataType: 'string',
          },
          {
            value: item.localityName,
            dataType: 'string',
          },
          { value: statusText, dataType: 'string' },
        ],
      };
    });
  }

  private updateFilters(partialFilters: Partial<PersonnelFilter>) {
    this.personnelFilters.update((f) => ({ ...f, ...partialFilters }));
    const orgId = this.selectedOrganizationId();
    if (orgId) this.getPersonnel(orgId);
  }

  onOrganizationChange(event: Event) {
    const target = event.target as HTMLSelectElement;
    const organizationId = target.value;
    if (!organizationId || organizationId === 'default') return;
    const id = Number(organizationId);
    this.selectedOrganizationId.set(id);
    this.getPersonnel(id);
  }

  onSearchChange(searchTerm: string) {
    this.updateFilters({ pageNumber: 1, searchTerm });
  }

  onFiltersChange(newFilters: PersonnelFilter) {
    this.updateFilters({ pageNumber: 1, ...newFilters });
  }

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
