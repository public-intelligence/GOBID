import { Component, inject, OnInit, signal } from '@angular/core';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import { headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { NotificationService, UserAppService } from '../../services';
import { firstValueFrom } from 'rxjs';
import { ColumnData, UserAdapted } from '../../models';
import { Router } from '@angular/router';

@Component({
  selector: 'app-user-app',
  imports: [Maintenance],
  templateUrl: './user-app.html',
})
export class UserApp implements OnInit {
  headers = headers;
  title = 'Listado de Relaciones Usuario-Aplicación';

  private layoutService = inject(LayoutService);
  createPath = '/user-application/create';

  private userAppService = inject(UserAppService);
  private notification = inject(NotificationService);
  private router = inject(Router);

  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);

  ngOnInit() {
    this.layoutService.setPageTitle('Listado de Relaciones Usuario-Aplicación');
    this.layoutService.breadcrumbs.set([
      { label: 'Relación Usuario-Aplicación', url: '/user-application' },
    ]);
    this.getUserApps();
  }

  formattedData(data: UserAdapted[]): ColumnData[] {
    return data.map((item) => ({
      id: String(item.id),
      name: item.personnel.firstName,
      showActions: false,
      actionsOptions: [
        {
          name: 'Vincular Aplicación',
          function: () => {
            this.router.navigate(['/user-application', item.userId]);
          },
        },
      ],
      columns: [
        { value: item.personnel.firstName, dataType: 'string' },
        {
          value: item.personnel.lastName,
          dataType: 'string',
        },
        { value: item.count.toString(), dataType: 'string', justify: 'center' },
      ],
    }));
  }

  async getUserApps() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.userAppService.findAllUserApplicationsCount(),
      );
      this.tableData.set(this.formattedData(response.data));
    } catch (_error) {
      this.notification.error(
        'Ha ocurrido un error al consultar las aplicaciones de usuario',
      );
    } finally {
      this.isLoading.set(false);
    }
  }
}
