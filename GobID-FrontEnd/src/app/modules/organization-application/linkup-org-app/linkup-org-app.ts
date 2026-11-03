import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { Maintenance } from '../../../components/ui/layouts/maintenance/maintenance.layout';
import { ActivatedRoute, Router } from '@angular/router';
import { faArrowLeft } from '@fortawesome/free-solid-svg-icons';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationFilter,
  ColumnData,
  OrganizationApplicationAdapted,
} from '../../../models';
import {
  NotificationService,
  OrganizationApplicationService,
  OrganizationService,
} from '../../../services';
import { headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-linkup-org-apps',
  imports: [Maintenance, FontAwesomeModule, ConfirmationModalComponent],
  templateUrl: './linkup-org-app.html',
})
export class LinkupOrgApps implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;
  private notification = inject(NotificationService);
  private organizationAppService = inject(OrganizationApplicationService);
  private organizationService = inject(OrganizationService);
  private applications = signal<OrganizationApplicationAdapted[]>([]);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  protected id = this.route.snapshot.paramMap.get('id');
  protected headers = headers;

  private layoutService = inject(LayoutService);
  protected title = signal<string>('');
  protected subTitle = 'Catálogo de Aplicaciones';
  protected faArrowLeft = faArrowLeft;
  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal(false);
  protected valueToSearch = signal('');
  protected appsFilter = signal<ApplicationFilter>({
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);

  ngOnInit() {
    this.layoutService.setPageTitle('Aplicaciones Vinculadas');
    this.layoutService.breadcrumbs.set([
      {
        label: 'Vinculación Organizacion-Aplicaciones',
        url: 'organization-application',
      },
      {
        label: 'Aplicaciones Vinculadas',
        url: `organization-application/organization/${this.id}`,
      },
    ]);
    this.getOrganizationById();
    this.getOrganizationApps();
  }

  /**
   *  Recupera todas las organizaciones.
   */
  getOrganizationApps = async () => {
    this.isLoading.set(true);
    try {
      const response = await firstValueFrom(
        this.organizationAppService.findAllByOrganizationId(
          this.id ?? '',
          this.appsFilter(),
        ),
      );
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      this.applications.set(response.data);
      this.tableData.set(this.formattedData(response.data));
    } catch (_error) {
      return;
    } finally {
      this.isLoading.set(false);
    }
  };

  /**
   *  Obtiene las organizaciones por ID.
   */
  getOrganizationById = async () => {
    try {
      const response = await firstValueFrom(
        this.organizationService.findOne(this.id ?? ''),
      );
      this.title.set(response.name);
    } catch (_error) {
      this.notification.error('Error al obtener los datos de la organización');
    }
  };

  /**
   * Formatea para la tabla.
   */
  formattedData(data: OrganizationApplicationAdapted[]): ColumnData[] {
    return data.map((item) => ({
      id: String(item.organizationApplicationId),
      name: item.application.name,
      showActions: false,
      actionsOptions: [
        {
          name: 'Editar',
          function: () => {
            this.editLinkedUpApp(item.applicationId.toString());
          },
        },
        {
          name: item.isActive
            ? 'Desvincular Aplicación'
            : 'Vincular Aplicación',
          function: () => {
            this.disableApp(item.organizationApplicationId.toString());
          },
        },
      ],
      columns: [
        { value: item.application.code, dataType: 'string' },
        { value: item.application.name, dataType: 'string' },
        {
          value: `${environment?.appUrl}/app/${item.applicationId?.toString()}/login`,
          dataType: 'link',
        },
        {
          value: item.isActive ? 'Activo' : 'Inactivo',
          dataType: 'string',
        },
      ],
    }));
  }

  /**
   *  Retorna a la Vista Anterior.
   */
  goBack() {
    this.router.navigate([`organization-application`]);
  }

  /**
   * Redirecciona a la vista de edición de la aplicación vinculada.
   */
  editLinkedUpApp(idApp: string) {
    this.router.navigate([
      `/organization-application/organization/${this.id}/linkup-app/edit/${idApp}`,
    ]);
  }

  /**
   *  Desvincula una aplicación
   */
  disableApp = async (id: string) => {
    try {
      const response = await firstValueFrom(
        this.organizationAppService.findOne(id),
      );
      const orgApp: OrganizationApplicationAdapted = {
        organizationApplicationId: response.organizationApplicationId,
        organizationId: response.organizationId,
        applicationId: response.applicationId,
        isActive: !response.isActive,
        organization: response.organization,
        application: response.application,
        role: response.role,
      };
      await firstValueFrom(this.organizationAppService.upsert(orgApp));
      this.notification.success(
        response.isActive
          ? 'Aplicación desvinculada de la organización correctamente'
          : 'Aplicación vinculada a la organización correctamente',
      );
      this.getOrganizationApps();
    } catch (_error) {
      this.notification.error('Error al realizar la operación');
    }
  };

  onDelete = async (id: string) => {
    try {
      await firstValueFrom(this.organizationAppService.delete(id));
      this.getOrganizationApps();
    } catch (_error) {
      this.notification.error(
        'Error al borrar el vínculo de la organización-aplicación',
      );
    }
  };

  showDeleteConfirmation(id: string, name: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Desvincular Aplicación',
      name,
      paragraph: `Está seguro(a) que desea desvincular la aplicación de la organización`,
      warning:
        'Al desvincular esta aplicación, se bloqueará el acceso de autenticación para todos los usuarios vinculados a ella.',
      confirmButton: 'Desvincular',
      onConfirm: () => {
        this.onDelete(id);
      },
    });
  }

  private updateFilters(partialFilters: Partial<ApplicationFilter>) {
    this.appsFilter.update((filters) => ({
      ...filters,
      ...partialFilters,
    }));
    this.getOrganizationApps();
  }

  onSearchChange(search: string) {
    this.updateFilters({ pageNumber: 1, search });
  }

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
