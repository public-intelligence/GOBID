import { Component, inject, OnInit, signal, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Maintenance } from '../../../components/ui/layouts/maintenance/maintenance.layout';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import { CreateCredentialModal } from './create-credential-modal/create-credential-modal';
import {
  AppCredential,
  ColumnData,
  getCredentialStatus,
} from '../../../models';
import { AppCredentialService } from '../../../services/app-credential/app-credential.service';
import {
  ApplicationService,
  NotificationService,
  UtilsService,
} from '../../../services';
import { headers } from './config';
import { LayoutService } from '@public-intelligence/ui-core';

@Component({
  selector: 'app-app-credentials',
  imports: [Maintenance, ConfirmationModalComponent, CreateCredentialModal],
  templateUrl: './app-credentials.html',
})
export class AppCredentials implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;
  @ViewChild(CreateCredentialModal) createModal!: CreateCredentialModal;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private credentialService = inject(AppCredentialService);
  private applicationService = inject(ApplicationService);
  private notification = inject(NotificationService);
  private utilsService = inject(UtilsService);

  protected applicationId = this.route.snapshot.params['id'];
  protected title = 'Credenciales M2M';
  protected headers = headers;

  private layoutService = inject(LayoutService);
  protected isLoading = signal(false);
  protected tableData = signal<ColumnData[]>([]);

  async ngOnInit() {
    this.layoutService.setPageTitle('Credenciales M2M');
    this.layoutService.breadcrumbs.set([
      { label: 'Aplicaciones', url: '/applications' },
      { label: 'Credenciales M2M', url: `/applications/${this.applicationId}/credentials` },
    ]);
    await this.loadAppName();
    await this.loadCredentials();
  }

  private async loadAppName() {
    try {
      const app = await firstValueFrom(
        this.applicationService.findOne(this.applicationId),
      );
      this.title = `Credenciales M2M — ${app.name}`;
    } catch {
      // El título queda por defecto si falla la carga
    }
  }

  async loadCredentials() {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.credentialService.list(this.applicationId),
      );
      this.tableData.set(this.formatCredentials(response.data));
    } catch {
      this.notification.error('Error al cargar las credenciales');
    } finally {
      this.isLoading.set(false);
    }
  }

  private formatCredentials(credentials: AppCredential[]): ColumnData[] {
    const statusLabels: Record<string, string> = {
      active: 'Activa',
      expired: 'Expirada',
      revoked: 'Revocada',
    };

    return credentials.map((cred) => {
      const status = getCredentialStatus(cred);
      const canRevoke = cred.isActive && !cred.isExpired;
      const daysLeft = Math.ceil(
        (new Date(cred.expiresAt).getTime() - Date.now()) / 86400000,
      );
      const expiryLabel =
        cred.expiresAt === null
          ? 'Nunca expira'
          : status === 'active' && daysLeft <= 30
            ? `${this.utilsService.formatDate(cred.expiresAt)} (${daysLeft}d)`
            : this.utilsService.formatDate(cred.expiresAt);

      return {
        id: cred.appCredentialId,
        name: cred.description ?? '—',
        showActions: false,
        actionsOptions: canRevoke
          ? [
              {
                name: 'Revocar',
                color: 'red' as const,
                function: () => this.showRevokeConfirmation(cred),
              },
            ]
          : [],
        columns: [
          { value: cred.description ?? '—', dataType: 'string' as const },
          {
            value: statusLabels[status],
            dataType: 'string' as const,
            justify: 'center' as const,
          },
          { value: expiryLabel, dataType: 'string' as const },
          {
            value: cred.lastUsedAt
              ? this.utilsService.formatDate(cred.lastUsedAt)
              : 'Nunca',
            dataType: 'string' as const,
          },
          {
            value: this.utilsService.formatDate(cred.createdAt),
            dataType: 'date' as const,
          },
        ],
      };
    });
  }

  showRevokeConfirmation(cred: AppCredential) {
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Revocar credencial',
      name: cred.description ?? cred.appCredentialId,
      paragraph: '¿Está seguro(a) que desea revocar la credencial',
      warning:
        'Al revocarla, ningún sistema podrá autenticarse con este secret. Los tokens activos permanecerán válidos hasta que expiren.',
      confirmButton: 'Revocar',
      onConfirm: () => this.onRevoke(cred.appCredentialId),
    });
  }

  async onRevoke(credentialId: string) {
    try {
      await firstValueFrom(
        this.credentialService.revoke(this.applicationId, credentialId),
      );
      this.notification.success('Credencial revocada correctamente');
      await this.loadCredentials();
    } catch {
      this.notification.error('Error al revocar la credencial');
    }
  }

  openCreateModal() {
    this.createModal.openModal();
  }

  goBack() {
    this.router.navigate(['/applications']);
  }
}
