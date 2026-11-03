import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { headers, filters } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { Maintenance } from '../../../components/ui/layouts/maintenance/maintenance.layout';
import { LegalEntityService, NotificationService } from '../../../services';
import { firstValueFrom } from 'rxjs';
import {
  LegalEntityAdapted,
  LegalEntityCreate,
  LegalEntityFilter,
  ColumnData,
  DisplayData,
} from '../../../models';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import { Router } from '@angular/router';

@Component({
  selector: 'app-legal-entity',
  imports: [Maintenance, ConfirmationModalComponent],
  templateUrl: './legal-entity.html',
})
export class LegalEntity implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  headers = headers();
  filters = filters;

  title = 'Listado de Entidades Legales';

  private layoutService = inject(LayoutService);
  createPath = '/identity-hub/legal-entity/create';
  editPath = (id: string) => `/identity-hub/legal-entity/edit/${id}`;

  private legalEntityService = inject(LegalEntityService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  private organizations = signal<LegalEntityAdapted[]>([]);

  protected filterArguments = signal<LegalEntityFilter>({
    pageNumber: 1,
    pageSize: 10,
  });

  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);
  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);

  ngOnInit(): void {
    this.layoutService.setPageTitle('Entidades Legales');
    this.layoutService.breadcrumbs.set([
      { label: 'Entidades Legales', url: '/identity-hub/legal-entity' },
    ]);
    this.getOrganizations();
  }

  async getOrganizations() {
    try {
      this.isLoading.set(true);
      const response: any = await firstValueFrom(
        this.legalEntityService.findAll(this.filterArguments() as any),
      );
      this.totalCount.set(response.totalCount || 0);
      this.totalPages.set(response.totalPages || 0);
      this.organizations.set(response.data || []);
      this.tableData.set(this.formattedData(response.data || []));
    } catch (_error) {
      this.notification.error('Error al consultar las organizaciones');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onDeleteOrganization(id: string) {
    try {
      await firstValueFrom(this.legalEntityService.delete(id));
      this.notification.success('Organización eliminada correctamente');
      this.getOrganizations();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al eliminar la organización');
    }
  }

  goToPath(path: string) {
    this.router.navigate([path]);
  }

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar organización',
      name,
      paragraph: 'Está seguro(a) que desea eliminar la organización',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeleteOrganization(id);
      },
    });
  }

  formattedDisplayData(data: LegalEntityAdapted): DisplayData[] {
    return [
      {
        columnName: 'Nombre Legal',
        value: data.legalName,
        type: 'string',
      },
      {
        columnName: 'Nombre Social',
        value: data.socialName,
        type: 'string',
      },
      {
        columnName: 'RNC',
        value: data.identificationNumber,
        type: 'string',
      },
      {
        columnName: 'Correo Electrónico',
        value: data.email,
        type: 'string',
      },
      {
        columnName: 'Teléfono',
        value: data.phoneNumber,
        type: 'string',
      },
      {
        columnName: 'Tipo',
        value: data.type,
        type: 'string',
      },
    ];
  }

  formattedData(data: LegalEntityAdapted[]): ColumnData[] {
    return data.map((item) => ({
      id: item.id.toString(),
      name: item.legalName,
      showActions: false,
      displayData: this.formattedDisplayData(item),
      editPath: this.editPath(item.id.toString()),
      actionsOptions: [
        {
          name: 'Editar',
          function: () => {
            this.goToPath(this.editPath(item.id.toString()));
          },
        },
        {
          name: 'Eliminar',
          color: 'red',
          function: () => {
            this.showDeleteConfirmation(item.id.toString(), item.legalName);
          },
        },
      ],
      columns: [
        { value: item.legalName, dataType: 'string' },
        { value: item.socialName, dataType: 'string' },
        { value: item.identificationNumber, dataType: 'string' },
        { value: item.email, dataType: 'string' },
        { value: item.phoneNumber, dataType: 'string' },
        { value: item.type, dataType: 'string' },
        { value: item.status, dataType: 'string' },
      ],
    }));
  }

  actualPage(page: number) {
    this.filterArguments.set({
      ...this.filterArguments(),
      pageNumber: page,
    });
    this.getOrganizations();
  }

  pageSize(size: number) {
    this.filterArguments.set({
      ...this.filterArguments(),
      pageSize: size,
    });
  }

  valueToSearchChange(value: string) {
    this.filterArguments.set({
      ...this.filterArguments(),
      pageNumber: 1,
      search: value,
    });
    this.getOrganizations();
  }

  statusValue(value: Partial<LegalEntityFilter>) {
    this.filterArguments.set({
      ...this.filterArguments(),
      pageNumber: 1,
      ...value,
    });
    this.getOrganizations();
  }
}
