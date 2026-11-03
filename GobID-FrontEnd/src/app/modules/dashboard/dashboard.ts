import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import {
  DashboardCardComponent,
  DashboardChartComponent,
} from '../../components/ui/dashboard';
import {
  DashboardCard,
  DashboardChart,
  DashboardActivity,
  monthlyActivity,
} from '../../models';
import { DashboardService } from '../../services/dashboard/dashboard.service';
import { LoadingSpinner } from '../../components/ui/loading-spinner/loading-spinner';
import { firstValueFrom, interval, Subject, takeUntil } from 'rxjs';
import { NotificationService, TokenDecoder } from '../../services';

@Component({
  selector: 'app-dashboard',
  imports: [DashboardCardComponent, DashboardChartComponent, LoadingSpinner],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit, OnDestroy {
  private dashboardService = inject(DashboardService);
  private destroy$ = new Subject<void>();
  private notification = inject(NotificationService);

  // Signals para el estado del componente
  protected isLoading = signal(true);
  protected hasError = signal(false);
  protected errorMessage = signal('');

  // Datos del dashboard
  protected cards = signal<DashboardCard[]>([]);
  protected charts = signal<DashboardChart[]>([]);
  protected recentActivity = signal<DashboardActivity[]>([]);
  private tokenDecoder = inject(TokenDecoder);

  protected permissions = this.tokenDecoder.getPermissions();
  protected isLocalAdmin = this.permissions.includes('admin_local');

  ngOnInit() {
    this.getMetrics();
    this.getuserConectionInfo();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  async getMetrics() {
    try {
      this.isLoading.set(true);
      this.hasError.set(false);
      const response = await firstValueFrom(this.dashboardService.getMetrics());

      const cards: DashboardCard[] = [
        {
          id: 'total-users',
          title: 'Total de Usuarios',
          value: response.totalUsers,
          subtitle: 'Usuarios registrados',
          icon: 'users',
          color: 'primary',
        },
        ...(this.isLocalAdmin
          ? []
          : [
              {
                id: 'total-organizations',
                title: 'Organizaciones',
                value: response?.totalOrganizations || 0,
                subtitle: 'Organizaciones registradas',
                icon: 'building',
                color: 'success' as const,
              },
            ]),
        {
          id: 'total-applications',
          title: 'Aplicaciones',
          value: response.totalApplications,
          subtitle: 'Apps registradas',
          icon: 'desktop',
          color: 'info',
        },
      ];

      this.cards.set(cards);
    } catch (_error) {
      this.notification.error('Error al cargar las informacion de la metrica');
    } finally {
      this.isLoading.set(false);
    }
  }

  async getuserConectionInfo() {
    try {
      this.isLoading.set(true);
      this.hasError.set(false);
      const response = await firstValueFrom(this.dashboardService.getChart());
      const info = response.data as monthlyActivity[];
      const charts: DashboardChart[] = [
        {
          id: 'monthly-activity',
          title: 'Actividad Mensual',
          type: 'bar',
          description: 'Accesos y autenticaciones por mes',
          data:
            info.map((item) => ({
              label: item.monthName,
              value: item.successfulLogins,
            })) || [],
        },
      ];
      this.charts.set(charts);
    } catch (_error) {
      this.notification.error('Error al cargar las informacion del grafico');
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Inicia las actualizaciones en tiempo real (cada 30 segundos)
   */
  private startRealtimeUpdates(): void {
    interval(10000 * 60 * 2) // 2 minutos
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.refreshData();
      });
  }

  /**
   * Refresca los datos sin mostrar el loading general
   */
  protected refreshData(): void {
    this.getMetrics();
    this.getuserConectionInfo();
  }

  /**
   * Recarga manualmente todos los datos
   */
  protected reloadDashboard(): void {
    this.getMetrics();
    this.getuserConectionInfo();
  }

  /**
   * Retry en caso de error
   */
  protected retryLoad(): void {
    this.getMetrics();
  }
}
