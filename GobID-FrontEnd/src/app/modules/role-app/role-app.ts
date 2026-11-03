import { Component, inject, OnInit, signal } from '@angular/core';
import { headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import {
  NotificationService,
  OrganizationApplicationService,
} from '../../services';
import { Router } from '@angular/router';
import {
  ColumnData,
  OrganizationApplicationAdapted,
  RolAppFilter,
} from '../../models';
import { firstValueFrom } from 'rxjs';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import { faEye } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-role-app',
  imports: [Maintenance],
  templateUrl: './role-app.html',
})
export class RoleApp implements OnInit {
  headers = headers;
  title = 'Listado de Aplicaciones';

  private layoutService = inject(LayoutService);

  private orgAppService = inject(OrganizationApplicationService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);
  protected rolAppFilter = signal<RolAppFilter>({
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);

  private allApplications: OrganizationApplicationAdapted[] = [];

  ngOnInit(): void {
    this.layoutService.setPageTitle('Vinculacion');
    this.layoutService.breadcrumbs.set([
      {
        label: 'Vinculacion role-aplicaciones',
        url: '/role-application',
      },
    ]);
    this.getApplications();
  }

  /////////////// Services /////////////////

  async getApplications() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.orgAppService.findAllApplicationByToken({
          pageNumber: 1,
          pageSize: 1000,
        }),
      );
      this.allApplications = response.data;
      this.applyLocalFilters();
    } catch (_error) {
      this.notification.error('Error al consultar las aplicaciones');
    } finally {
      this.isLoading.set(false);
    }
  }

  private applyLocalFilters() {
    const { search, pageNumber = 1, pageSize = 10 } = this.rolAppFilter();

    const filtered = search
      ? this.allApplications.filter((item) =>
          [item.application.name, item.application.code]
            .join(' ')
            .toLowerCase()
            .includes(search.toLowerCase()),
        )
      : this.allApplications;

    const totalCount = filtered.length;
    const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    const start = (pageNumber - 1) * pageSize;
    const paginated = filtered.slice(start, start + pageSize);

    this.totalCount.set(totalCount);
    this.totalPages.set(totalPages);
    this.tableData.set(this.formattedData(paginated));
  }

  ////////////// Helpers /////////////////////

  formattedData(data: OrganizationApplicationAdapted[]): ColumnData[] {
    return data.map((item) => ({
      id: String(item.organizationApplicationId),
      name: item.application.name,
      showActions: false,
      actionsOptions: [
        {
          name: 'Ver roles',
          icon: faEye,
          function: () =>
            this.router.navigate([
              `/role-application/${item.organizationApplicationId}/roles`,
            ]),
        },
      ],
      columns: [
        { value: item.application.code, dataType: 'string' },
        { value: item.application.name, dataType: 'string' },
        {
          value: item.roleCount?.toString() ?? '0',
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
    this.applyLocalFilters();
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
