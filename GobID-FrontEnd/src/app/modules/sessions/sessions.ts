import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';
import { Maintenance } from '../../components/ui/layouts/maintenance/maintenance.layout';
import { ApplicationService, NotificationService } from '../../services';
import { SessionService } from '../../services/session/session.service';
import { firstValueFrom } from 'rxjs';
import {
  ApplicationAdapted,
  ColumnData,
  DisplayData,
  Filter,
  SessionFilter,
  UserSession,
} from '../../models';
import { ConfirmationModalComponent } from '../../components/ui/modals/confirmation-modal/confirmation-modal';

@Component({
  selector: 'app-sessions',
  imports: [Maintenance, ConfirmationModalComponent],
  templateUrl: './sessions.html',
})
export class Sessions implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  headers = headers;
  title = 'Listado de Sesiones Activas';

  private layoutService = inject(LayoutService);

  private sessionService = inject(SessionService);
  private applicationService = inject(ApplicationService);
  private notification = inject(NotificationService);

  protected tableData = signal<ColumnData[]>([]);
  protected isLoading = signal<boolean>(false);
  protected sessionFilters = signal<SessionFilter>({
    IsActive: 'true',
    pageNumber: 1,
    pageSize: 10,
  });
  protected totalCount = signal<number>(0);
  protected totalPages = signal<number>(0);
  protected appFilters = signal<Filter[]>([]);

  ngOnInit(): void {
    this.layoutService.setPageTitle('Sesiones Activas');
    this.layoutService.breadcrumbs.set([
      { label: 'Sesiones', url: '/sessions' },
    ]);
    this.getSessions();
    this.loadApplicationFilters();
  }

  /////////////// Services /////////////////

  async getSessions() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.sessionService.findAll(this.sessionFilters()),
      );
      this.totalCount.set(response.totalCount);
      this.totalPages.set(response.totalPages);
      this.tableData.set(this.formattedData(response.data as UserSession[]));
    } catch (_error) {
      this.notification.error('Error al consultar las sesiones');
    } finally {
      this.isLoading.set(false);
    }
  }

  async loadApplicationFilters() {
    try {
      const response = await firstValueFrom(
        this.applicationService.findAll({ IsActive: 'true', pageSize: 100 }),
      );
      const appOptions = [
        { label: 'Todas las aplicaciones', value: '' },
        ...response.data.map((app: ApplicationAdapted) => ({
          label: app.name,
          value: app.id.toString(),
        })),
      ];
      this.appFilters.set([
        {
          title: 'Aplicación',
          values: appOptions,
          controlName: 'ApplicationId',
          defaultValue: '',
          type: 'select',
        },
      ]);
    } catch (_error) {
      this.notification.error('Error al cargar las aplicaciones');
    }
  }

  async onRevokeSession(sessionId: string) {
    try {
      await firstValueFrom(this.sessionService.revoke(sessionId));
      this.notification.success('Sesión revocada correctamente');
      this.getSessions();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al revocar la sesión');
    }
  }

  async onRevokeAll() {
    try {
      await firstValueFrom(this.sessionService.revokeAll());
      this.notification.success('Todas las sesiones han sido revocadas');
      this.getSessions();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al revocar las sesiones');
    }
  }

  async onRevokeByApplication(applicationId: string) {
    try {
      await firstValueFrom(
        this.sessionService.revokeByApplication(applicationId),
      );
      this.notification.success(
        'Sesiones de la aplicación revocadas correctamente',
      );
      this.getSessions();
      this.confirmationModal.closeModal();
    } catch (_error) {
      this.notification.error('Error al revocar las sesiones de la aplicación');
    }
  }

  /////////////// Helpers /////////////////

  formatDateTime(date: string): string {
    const d = new Date(date);
    const day = String(d.getUTCDate()).padStart(2, '0');
    const month = String(d.getUTCMonth() + 1).padStart(2, '0');
    const year = d.getUTCFullYear();
    const hours = String(d.getUTCHours()).padStart(2, '0');
    const minutes = String(d.getUTCMinutes()).padStart(2, '0');
    return `${day}-${month}-${year} ${hours}:${minutes}`;
  }

  get selectedApplicationId(): string {
    return this.sessionFilters().ApplicationId ?? '';
  }

  showRevokeConfirmation(sessionId: string, email: string) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Revocar sesión',
      name: email,
      paragraph: 'Está seguro(a) que desea revocar la sesión del usuario',
      confirmButton: 'Revocar',
      onConfirm: () => {
        this.onRevokeSession(sessionId);
      },
    });
  }

  showRevokeAllConfirmation() {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Revocar todas las sesiones',
      name: 'todas las sesiones activas',
      paragraph:
        'Está seguro(a) que desea revocar todas las sesiones activas del sistema',
      confirmButton: 'Revocar todas',
      onConfirm: () => {
        this.onRevokeAll();
      },
    });
  }

  showRevokeByAppConfirmation() {
    const applicationId = this.selectedApplicationId;
    const appName =
      this.appFilters()
        .find((f) => f.controlName === 'ApplicationId')
        ?.values?.find((v) => v.value === applicationId)?.label ??
      applicationId;

    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Revocar sesiones de la aplicación',
      name: appName,
      paragraph:
        'Está seguro(a) que desea revocar todas las sesiones activas de la aplicación',
      confirmButton: 'Revocar',
      onConfirm: () => {
        this.onRevokeByApplication(applicationId);
      },
    });
  }

  formattedDisplayData(item: UserSession): DisplayData[] {
    const fullName =
      item.firstName || item.lastName
        ? `${item.firstName ?? ''} ${item.lastName ?? ''}`.trim()
        : 'Sin nombre';

    return [
      { columnName: 'Session ID', value: item.sessionId, type: 'string' },
      { columnName: 'Email', value: item.email, type: 'string' },
      { columnName: 'Nombre', value: fullName, type: 'string' },
      {
        columnName: 'Número de Identificación',
        value: item.identificationNumber ?? '-',
        type: 'string',
      },
      { columnName: 'IP', value: item.ip, type: 'string' },
      { columnName: 'User Agent', value: item.userAgent, type: 'string' },
      {
        columnName: 'Dispositivo Confiable',
        value: item.isTrusted ? 'Sí' : 'No',
        type: 'string',
      },
      {
        columnName: 'Fecha de Creación',
        value: this.formatDateTime(item.createdAt),
        type: 'date',
      },
      {
        columnName: 'Última Actividad',
        value: this.formatDateTime(item.lastActivityAt),
        type: 'date',
      },
      {
        columnName: 'Expira',
        value: this.formatDateTime(item.absoluteExpiresAt),
        type: 'date',
      },
    ];
  }

  formattedData(data: UserSession[]): ColumnData[] {
    return data.map((item) => {
      const fullName =
        item.firstName || item.lastName
          ? `${item.firstName ?? ''} ${item.lastName ?? ''}`.trim()
          : 'Sin nombre';

      return {
        id: item.sessionId,
        name: item.email,
        showActions: false,
        displayData: this.formattedDisplayData(item),
        actionsOptions: [
          {
            name: 'Revocar',
            color: 'red',
            function: () => {
              this.showRevokeConfirmation(item.sessionId, item.email);
            },
          },
        ],
        columns: [
          { value: item.email, dataType: 'string' },
          { value: fullName, dataType: 'string' },
          { value: item.ip, dataType: 'string' },
          { value: item.applicationId, dataType: 'string' },
          {
            value: this.formatDateTime(item.lastActivityAt),
            dataType: 'string',
          },
          {
            value: this.formatDateTime(item.absoluteExpiresAt),
            dataType: 'string',
          },
        ],
      };
    });
  }

  private updateFilters(partialFilters: Partial<SessionFilter>) {
    this.sessionFilters.update((f) => ({ ...f, ...partialFilters }));
    this.getSessions();
  }

  onSearchChange(search: string) {
    this.updateFilters({ pageNumber: 1, search });
  }

  onFiltersChange(filters: SessionFilter) {
    this.updateFilters({ pageNumber: 1, ...filters });
  }

  onPageChange(pageNumber: number) {
    this.updateFilters({ pageNumber });
  }

  onPageSizeChange(pageSize: number) {
    this.updateFilters({ pageSize, pageNumber: 1 });
  }
}
