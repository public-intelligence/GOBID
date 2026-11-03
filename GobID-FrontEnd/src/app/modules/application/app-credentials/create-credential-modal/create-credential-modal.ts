import {
  Component,
  EventEmitter,
  inject,
  Input,
  Output,
  signal,
  ViewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { NgClass } from '@angular/common';
import { faCheck, faCopy } from '@fortawesome/free-solid-svg-icons';
import { ModalComponent } from '../../../../components/shared/modal/modal';
import { InputComponent } from '../../../../components/shared/input/input';
import { MultiSelectComponent, MultiSelectOption } from '../../../../components/shared/multiselect/multiselect';
import { AppCredentialCreated, CreateAppCredentialRequest, OrganizationApplicationAdapted } from '../../../../models';
import { AppCredentialService } from '../../../../services/app-credential/app-credential.service';
import { ClaimService, NotificationService, OrganizationApplicationService } from '../../../../services';

@Component({
  selector: 'app-create-credential-modal',
  imports: [ModalComponent, ReactiveFormsModule, InputComponent, MultiSelectComponent, FontAwesomeModule, NgClass],
  templateUrl: './create-credential-modal.html',
})
export class CreateCredentialModal {
  @Input() applicationId!: string;
  @Output() created = new EventEmitter<void>();
  @ViewChild(ModalComponent) modal!: ModalComponent;

  protected faCopy = faCopy;
  protected faCheck = faCheck;

  protected isPending = signal(false);
  protected createdCredential = signal<AppCredentialCreated | null>(null);
  protected secretCopied = signal(false);
  protected clientIdCopied = signal(false);
  protected claimsLoading = signal(false);
  protected claimOptions = signal<MultiSelectOption[]>([]);
  protected orgAppLoading = signal(false);
  protected orgAppOptions = signal<OrganizationApplicationAdapted[]>([]);

  private fb = inject(FormBuilder);
  private credentialService = inject(AppCredentialService);
  private claimService = inject(ClaimService);
  private orgAppService = inject(OrganizationApplicationService);
  private notification = inject(NotificationService);

  form = this.fb.group({
    description: [''],
    expiryDays: [90, [Validators.min(0), Validators.max(365)]],
    claimIds: [[] as number[]],
    organizationApplicationId: [null as number | null, Validators.required],
  });

  async openModal() {
    this.form.reset({ description: '', expiryDays: 90, claimIds: [], organizationApplicationId: null });
    this.createdCredential.set(null);
    this.secretCopied.set(false);
    this.clientIdCopied.set(false);
    this.modal.toggleModal(true);
    await Promise.all([this.loadClaims(), this.loadOrgApps()]);
  }

  closeModal() {
    if (this.createdCredential()) {
      this.created.emit();
    }
    this.modal.toggleModal(false);
    this.createdCredential.set(null);
  }

  async onSubmit() {
    if (this.form.invalid) return;

    const claimIds = this.form.value.claimIds;
    const body: CreateAppCredentialRequest = {
      description: this.form.value.description || undefined,
      expiryDays: this.form.value.expiryDays ?? undefined,
      claimIds: claimIds && claimIds.length > 0 ? claimIds : undefined,
      organizationApplicationId: this.form.value.organizationApplicationId ?? undefined,
    };

    try {
      this.isPending.set(true);
      const response = await firstValueFrom(
        this.credentialService.create(this.applicationId, body),
      );
      this.createdCredential.set(response.data);
    } catch (error) {
      this.notification.error(
        (error as { message?: string }).message ?? 'Error al crear la credencial',
      );
    } finally {
      this.isPending.set(false);
    }
  }

  private async loadOrgApps() {
    try {
      this.orgAppLoading.set(true);
      const response = await firstValueFrom(
        this.orgAppService.findAllByApplicationOrganizationByApplicationId(this.applicationId),
      );
      this.orgAppOptions.set(response.data);
    } catch {
      this.orgAppOptions.set([]);
    } finally {
      this.orgAppLoading.set(false);
    }
  }

  private async loadClaims() {
    try {
      this.claimsLoading.set(true);
      const response = await firstValueFrom(
        this.claimService.findAll({ applicationId: this.applicationId }),
      );
      this.claimOptions.set(
        response.data.map((c) => ({ id: c.id as number, label: c.name })),
      );
    } catch {
      this.claimOptions.set([]);
    } finally {
      this.claimsLoading.set(false);
    }
  }

  copySecret() {
    const cred = this.createdCredential();
    if (!cred) return;
    navigator.clipboard.writeText(cred.clientSecret).then(() => {
      this.secretCopied.set(true);
    });
  }

  copyClientId() {
    const cred = this.createdCredential();
    if (!cred) return;
    navigator.clipboard.writeText(cred.appCredentialId).then(() => {
      this.clientIdCopied.set(true);
    });
  }

  formatDate(date: string): string {
    const d = new Date(date);
    const day = String(d.getUTCDate()).padStart(2, '0');
    const month = String(d.getUTCMonth() + 1).padStart(2, '0');
    const year = d.getUTCFullYear();
    return `${day}/${month}/${year}`;
  }
}
