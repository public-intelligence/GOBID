import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { headers, filters } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { Maintenance } from '../../../components/ui/layouts/maintenance/maintenance.layout';
import { PersonService, NotificationService } from '../../../services';
import { firstValueFrom } from 'rxjs';
import {
  PersonAdapted,
  PersonCreate,
  PersonFilter,
  ColumnData,
  DisplayData,
} from '../../../models';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import { Router } from '@angular/router';

@Component({
  selector: 'app-person',
  imports: [Maintenance, ConfirmationModalComponent],
  templateUrl: './person.html',
})
export class Person implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  title = 'Listado de Personas';

  private layoutService = inject(LayoutService);
  createPath = '/identity-hub/person/create';
  editPath = (id: string) => `/identity-hub/person/edit/${id}`;
  headers = headers();
  filters = filters;

  private personService = inject(PersonService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  private persons = signal<PersonAdapted[]>([]);

  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);
  protected valueToSearch = signal('');

  protected personFilter = signal<PersonFilter>({
    status: '1',
    pageNumber: 1,
    pageSize: 10,
  });

  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);

  ngOnInit(): void {
    this.layoutService.setPageTitle('Personas');
    this.layoutService.breadcrumbs.set([
      { label: 'Personas', url: '/identity-hub/person' },
    ]);
    this.getPersons();
  }

  async getPersons() {
    try {
      this.isLoading.set(true);
      const response: any = await firstValueFrom(
        this.personService.findAll(this.personFilter() as any),
      );
      this.totalCount.set(response.totalCount || 0);
      this.totalPages.set(response.totalPages || 0);
      this.persons.set(response.data || []);
      this.tableData.set(this.formattedData(response.data || []));
    } catch (_error) {
      this.notification.error('Error al consultar el personal');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onDeletePerson(id: string) {
    try {
      await firstValueFrom(this.personService.delete(id));
      this.notification.success('Personal eliminado correctamente');
      this.getPersons();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al eliminar el personal');
    }
  }

  async onUpdateStatusPerson(
    id: string,
    data: Pick<PersonCreate, 'id' | 'status'>,
  ) {
    try {
      await firstValueFrom(this.personService.update(id, data));
      this.notification.success('Personal actualizado correctamente');
      this.getPersons();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al actualizar el personal');
    }
  }

  goToEdit(id: string) {
    this.router.navigate([this.editPath(id)]);
  }

  formattedDataToActivateOrDisable(
    data: PersonAdapted,
    status: string,
  ): Pick<PersonCreate, 'id' | 'status'> {
    return {
      id: data.id,
      status: status,
    };
  }

  formattedDisplayData(data: PersonAdapted): DisplayData[] {
    return [
      {
        columnName: 'nombre',
        value: data.firstName,
        type: 'string',
      },
      {
        columnName: 'apellido',
        value: data.lastName,
        type: 'string',
      },
      {
        columnName: 'cédula',
        value: data.identificationNumber,
        type: 'string',
      },
      {
        columnName: 'email',
        value: data.email,
        type: 'string',
      },
      {
        columnName: 'teléfono',
        value: data.phoneNumber,
        type: 'string',
      },
      {
        columnName: 'estado',
        value: data.status,
        type: 'string',
      },
    ];
  }

  formattedData(data: PersonAdapted[]): ColumnData[] {
    return data.map((item: PersonAdapted) => ({
      id: String(item.id),
      name: item.firstName + ' ' + item.lastName,
      showActions: false,
      displayData: this.formattedDisplayData(item),
      editPath: this.editPath(item.id.toString()),
      actionsOptions: [
        {
          name: 'Editar',
          function: () => {
            this.goToEdit(item.id.toString());
          },
        },
        item.status === 'Active'
          ? {
              name: 'Desactivar',
              color: 'orange',
              function: () => {
                this.showActivateConfirmation(
                  item.id.toString(),
                  this.formattedDataToActivateOrDisable(item, 'Inactive'),
                  item.firstName + ' ' + item.lastName,
                  'deactivate',
                );
              },
            }
          : {
              name: 'Activar',
              color: 'green',
              function: () => {
                this.showActivateConfirmation(
                  item.id.toString(),
                  this.formattedDataToActivateOrDisable(item, 'Active'),
                  item.firstName + ' ' + item.lastName,
                  'activate',
                );
              },
            },
        {
          name: 'Eliminar',
          color: 'red',
          function: () => {
            this.showDeleteConfirmation(
              item.id.toString(),
              item.firstName + ' ' + item.lastName,
            );
          },
        },
      ],
      columns: [
        { value: item.firstName, dataType: 'string' as const },
        { value: item.lastName, dataType: 'string' as const },
        { value: item.identificationNumber, dataType: 'string' as const },
        { value: item.email, dataType: 'string' as const },
        { value: item.phoneNumber, dataType: 'string' as const },
        {
          value: item.status,
          dataType: 'string' as const,
        },
      ],
    }));
  }

  showActivateConfirmation(
    id: string,
    data: Pick<PersonCreate, 'id' | 'status'>,
    name: string,
    status: 'activate' | 'deactivate' = 'activate',
  ) {
    this.confirmationModal.openModal({
      title: status === 'activate' ? 'Activar personal' : 'Desactivar personal',
      name,
      paragraph: `Está seguro(a) que desea ${
        status === 'activate' ? 'activar' : 'desactivar'
      } el personal`,
      confirmButton: status === 'activate' ? 'Activar' : 'Desactivar',
      onConfirm: () => {
        this.onUpdateStatusPerson(id, data);
      },
    });
  }

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar personal',
      name,
      paragraph: `Está seguro(a) que desea eliminar el personal`,
      warning: 'Esta acción no se puede deshacer.',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeletePerson(id);
      },
    });
  }

  valueToSearchChange(value: string) {
    this.valueToSearch.set(value);
    this.personFilter.set({
      ...this.personFilter(),
      pageNumber: 1,
      search: this.valueToSearch(),
    });
    this.getPersons();
  }

  statusValue(value: PersonFilter) {
    this.personFilter.set({
      ...this.personFilter(),
      pageNumber: 1,
      ...value,
    });
    this.getPersons();
  }

  actualPage(page: number) {
    this.personFilter.set({
      ...this.personFilter(),
      pageNumber: page,
    });
    this.getPersons();
  }

  pageSize(size: number) {
    this.personFilter.set({
      ...this.personFilter(),
      pageSize: size,
    });
  }
}
