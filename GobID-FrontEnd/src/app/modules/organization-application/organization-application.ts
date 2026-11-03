import { Component, inject, OnInit, signal } from '@angular/core';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import { headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import {
  ColumnData,
  OrganizationFilter,
  OrganizationAdapted,
} from '../../models';
import { OrganizationService } from '../../services/organization/organization.service';
import { firstValueFrom } from 'rxjs';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faLink, faEye } from '@fortawesome/free-solid-svg-icons';
import { Router } from '@angular/router';
import { NotificationService } from '../../services';

@Component({
  selector: 'app-organization-application',
  imports: [Maintenance, FontAwesomeModule],
  templateUrl: './organization-application.html',
})
export class OrganizationApplication implements OnInit {
  private router = inject(Router);
  private notification = inject(NotificationService);
  private organizationService = inject(OrganizationService);

  protected headers = headers;
  protected title = 'Listado de Organizaciones';

  private layoutService = inject(LayoutService);
  protected faLink = faLink;
  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal(false);
  protected organizationFilter = signal<OrganizationFilter>({
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);

  ngOnInit() {
    this.layoutService.setPageTitle('Vinculación');
    this.layoutService.breadcrumbs.set([
      { label: 'Vinculación Organizacion-Aplicaciones', url: '' },
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
      this.notification.error(
        'Error al obtener el listado de las organizaciones',
      );
    } finally {
      this.isLoading.set(false);
    }
  };

  /**
   * Formatea para la tabla.
   */
  formattedData(data: OrganizationAdapted[]): ColumnData[] {
    return data.map((item) => ({
      id: String(item.id),
      name: item.name,
      showActions: false,
      actionsOptions: [
        {
          name: 'Aplicaciones Vinculadas',
          icon: faEye,
          function: () => {
            this.onNavigateTo(item.id.toString());
          },
        },
      ],
      columns: [
        { value: item.name, dataType: 'string' },
        { value: item.acronym, dataType: 'string' },
        { value: item.count.toString(), dataType: 'number', justify: 'center' },
      ],
    }));
  }

  /**
   * Navega a la página de creación de una nueva organización.
   */
  onNavigateTo = (id: string) => {
    this.router.navigate([`/organization-application/organization/${id}`]);
  };

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

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
