import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal';
import { Router } from '@angular/router';
import { NotificationService, TokenDecoder } from '../../services';
import { filters, headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { faLink } from '@fortawesome/free-solid-svg-icons';
import { ColumnData, DisplayData } from '../../models';
import { firstValueFrom } from 'rxjs';
import { PolicyService } from '../../services/policy/policy.service';
import { PolicyAdapted, PolicyFilter, Policy } from '../../models/policy';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';

@Component({
  selector: 'app-policy',
  imports: [ConfirmationModalComponent, Maintenance],
  templateUrl: './policy.html',
})
export class PolicyComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;
  private router = inject(Router);
  private notification = inject(NotificationService);
  private policyService = inject(PolicyService);
  private policies = signal<PolicyAdapted[]>([]);
  private tokenService = inject(TokenDecoder);

  protected headers = headers;
  protected filters = filters;
  protected title = 'Listado de Políticas';

  private layoutService = inject(LayoutService);
  protected faLink = faLink;
  protected editPath = (id: string | number) => `/policy/edit/${id}`;
  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal(false);
  protected policyFilter = signal<PolicyFilter>({
    IsActive: 'true',
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);

  ngOnInit() {
    this.layoutService.setPageTitle('Políticas');
    this.layoutService.breadcrumbs.set([
      { label: 'Políticas', url: '/policy' },
    ]);
    this.getPolicies();
  }

  /**
   *  Recupera todas las organizaciones.
   */
  getPolicies = async () => {
    this.isLoading.set(true);
    try {
      const organizationId = this.tokenService.getOrganizationId();
      const response = await firstValueFrom(
        this.policyService.findAllByOrganization(
          organizationId,
          this.policyFilter(),
        ),
      );
      this.policies.set(response.data);
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      this.tableData.set(this.formattedData(this.policies()));
    } catch (_error) {
      this.notification.error('Error al consultar las Políticas');
    } finally {
      this.isLoading.set(false);
    }
  };

  formattedDisplayData(data: PolicyAdapted): DisplayData[] {
    return [
      {
        columnName: 'Nombre de la Política',
        value: data.name,
        type: 'string',
      },
      {
        columnName: 'Descripción',
        value: data.description ?? '',
        type: 'string',
      },
      {
        columnName: 'efecto',
        value: data.effect,
        type: 'string',
      },
      {
        columnName: 'estado',
        value: data.isActive ? 'Activo' : 'Inactivo',
        type: 'boolean',
      },
      {
        columnName: 'Condiciones',
        value: data.conditions?.length.toString() ?? '0',
        type: 'string',
      },
    ];
  }

  /**
   * Formatea para la tabla.
   */
  formattedData(data: PolicyAdapted[]): ColumnData[] {
    return data.map((item) => {
      const currentId = item.policyId.toString();
      const isActive = item.isActive;
      const statusText = isActive ? 'Activo' : 'Inactivo';

      return {
        id: currentId,
        name: item.name,
        editPath: this.editPath(currentId),
        showActions: false,
        displayData: this.formattedDisplayData(item),
        actionsOptions: [
          {
            name: 'Editar',
            function: () => this.onEditPolicy(currentId),
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
            value: item.name,
            dataType: 'string',
          },
          {
            value: item.description ?? '',
            dataType: 'string',
          },
          {
            value:
              item.effect === 'DENY' ? 'Denegar acceso' : 'Permitir acceso',
            dataType: 'string',
          },
          { value: statusText, dataType: 'string' },
        ],
      };
    });
  }

  onEditPolicy = (id: string) => {
    this.router.navigate([`/policy/edit/${id}`]);
  };

  /**
   * Elimina una organizació.
   * @param id Identificador de la politica a desactivar/activar.
   */
  onDeletePolicy = async (id: string) => {
    try {
      const response = await firstValueFrom(this.policyService.findOne(id));
      await firstValueFrom(
        this.policyService.delete(response.policyId.toString()),
      );
      this.getPolicies();
    } catch (_error) {
      this.notification.error('Error al desactivar la política');
    }
  };

  showActivateOrDisableConfirmation(
    item: PolicyAdapted,
    action: 'activate' | 'deactivate',
  ) {
    const isActivate = action === 'activate';
    const titleActionName = isActivate ? 'Activar' : 'Desactivar';
    const confirmationActionName = isActivate ? 'activar' : 'desactivar';

    this.confirmationModal.openModal({
      title: `${titleActionName} politica`,
      name: item.name,
      paragraph: `¿Está seguro(a) que desea ${confirmationActionName} la politica?`,
      confirmButton: titleActionName,
      onConfirm: () => {
        this.onActiveOrganization(
          {
            policyId: item.policyId,
            organizationId: item.organizationId,
            policyTypeId: item.policyTypeId,
            name: item.name,
            description: item.description,
            effect: item.effect,
            isActive: isActivate,
          },
          isActivate,
        );
      },
    });
  }

  onActiveOrganization = async (data: PolicyAdapted, status: boolean) => {
    try {
      const updatedOrganization: PolicyAdapted = {
        policyId: data.policyId,
        policyTypeId: data.policyTypeId,
        name: data.name,
        description: data.description,
        organizationId: data.organizationId,
        effect: data.effect,
        isActive: status,
      };
      await firstValueFrom(this.policyService.upsert(updatedOrganization));
      this.getPolicies();
      this.notification.success(
        !status
          ? 'Política desactivada correctamente'
          : 'Política activada correctamente',
      );
    } catch (_error) {
      this.notification.error('Error al realizar la operación');
    }
  };

  /**
   * Navega a la página de creación de una nueva politica.
   */
  onNavigateTo = () => {
    this.router.navigate(['/policy/create']);
  };

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar politica',
      name,
      paragraph: `Está seguro(a) que desea eliminar la politica`,
      warning:
        'Al desactivar esta politica, se bloqueará el acceso de autenticación para todos los usuarios vinculados a ella.',
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeletePolicy(id);
      },
    });
  }

  private updateFilters(partialFilters: Partial<PolicyFilter>) {
    this.policyFilter.update((filters) => ({
      ...filters,
      ...partialFilters,
    }));
    this.getPolicies();
  }

  onSearchChange(search: string) {
    this.updateFilters({ pageNumber: 1, search });
  }

  onFiltersChange(filters: PolicyFilter) {
    this.updateFilters({ pageNumber: 1, ...filters });
  }

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
