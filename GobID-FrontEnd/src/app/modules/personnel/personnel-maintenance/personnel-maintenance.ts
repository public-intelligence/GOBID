import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LayoutService } from '@public-intelligence/ui-core';
import { firstValueFrom } from 'rxjs';
import {
  CheckboxComponent,
  SearchComponent,
  CustomSelectSearch,
  PhotoInputComponent,
} from '../../../components/shared';
import {
  OrganizationAdapted,
  PersonAdapted,
  PersonnelAdapted,
  Position,
  Select,
} from '../../../models';
import {
  NotificationService,
  OrganizationService,
  PersonService,
  TokenDecoder,
} from '../../../services';
import { PersonnelService } from '../../../services/personnel/personnel.service';
import { PositionService } from '../../../services/position/position.service';
import { noDefaultValidator, samePositionValidator } from '../../../validators';

@Component({
  selector: 'app-personnel-maintenance',
  imports: [
    ReactiveFormsModule,
    CheckboxComponent,
    SearchComponent,
    CustomSelectSearch,
    PhotoInputComponent,
  ],
  templateUrl: './personnel-maintenance.html',
})
export class PersonnelMaintenance implements OnInit {
  private layoutService = inject(LayoutService);
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private notification = inject(NotificationService);
  private personnelService = inject(PersonnelService);
  private organizationService = inject(OrganizationService);
  private positionService = inject(PositionService);
  private personService = inject(PersonService);
  private tokenDecoder = inject(TokenDecoder);
  private permissions = this.tokenDecoder.getPermissions();
  private currentPositionOption = signal<Select | null>(null);
  private currentPositionNominateOption = signal<Select | null>(null);

  protected isLoading = signal(false);
  protected isPending = signal(false);
  protected isSubmitted = signal(false);
  protected organizationOptions = signal<Select[]>([]);
  protected positionOptions = signal<Select[]>([]);

  protected positionOptionsWithCurrent = computed(() => {
    const current = this.currentPositionOption();
    const opts = this.positionOptions();
    if (!current) return opts;
    return opts.some((p) => String(p.value) === String(current.value))
      ? opts
      : [current, ...opts];
  });
  
  protected positionNominateOptionsWithCurrent = computed(() => {
    const current = this.currentPositionNominateOption();
    const opts = this.positionOptions();
    if (!current) return opts;
    return opts.some((p) => String(p.value) === String(current.value))
      ? opts
      : [current, ...opts];
  });

  protected localityOptions = signal<Select[]>([]);
  private currentLocalityOption = signal<Select | null>(null);
  protected localityOptionsWithCurrent = computed(() => {
    const current = this.currentLocalityOption();
    const opts = this.localityOptions();
    if (!current) return opts;
    return opts.some((p) => String(p.value) === String(current.value))
      ? opts
      : [current, ...opts];
  });
  protected foundPerson = signal<PersonAdapted | null>(null);
  protected searchErrMsg = signal('');
  protected photoControl = new FormControl<File | string | null>(null);
  protected isUploadingPhoto = signal(false);
  protected isSuperAdmin = this.permissions.includes('super_admin');

  private id = this.activatedRoute.snapshot.paramMap.get('id') as string;
  protected isEdit = !!this.id;

  fb = new FormBuilder();

  form = this.fb.group({
    personnelId: [''],
    personId: ['', Validators.required],
    organizationId: ['default', [Validators.required, noDefaultValidator]],
    positionId: ['', samePositionValidator],
    positionNominateId: ['', Validators.required],
    localityId: [''],
    isActive: [true],
  });

  ngOnInit(): void {
    this.layoutService.setPageTitle('Mantenimiento de Personal');
    this.layoutService.breadcrumbs.set([
      { label: 'Personal', url: '/personnel' },
      { label: 'Mantenimiento', url: '/personnel/maintenance' },
    ]);
    const orgId = this.tokenDecoder.getOrganizationId();
    this.form.get('organizationId')?.setValue(orgId);
    this.form.get('organizationId')?.disable();
    this.getPositions();
    this.getLocalities();
    if (this.isEdit) this.getPersonnelById();
  }

  async getPositions(q = '') {
    try {
      const response = await firstValueFrom(
        this.positionService.search(q, { pageSize: 200 }),
      );
      const raw = (response?.data ?? response ?? []) as {
        id: string;
        name: string;
      }[];
      const options = raw.map((pos) => ({
        label: pos.name,
        value: pos.id,
      }));
      this.positionOptions.set(options);
    } catch (_error) {
      this.notification.error('Error al consultar las posiciones');
    }
  }

  async getPositionsById(id: string) {
    try {
      const response = await firstValueFrom(
        this.positionService.findOne(id),
      );
      const options = {
        label: response.data?.name,
        value: response.data?.id,
      };
      return options;
    } catch (_error) {
      this.notification.error('Error al consultar las posiciones');
      return null;
    }
  }

  protected onSearchPositionsFilter(value: string | number): void {
    this.getPositions(String(value));
  }

  async getLocalities(q = '') {
    try {
      const orgId = this.tokenDecoder.getOrganizationId();
      const response = await firstValueFrom(
        this.organizationService.locality(orgId, q, { pageSize: 200 }),
      );
      const raw = (response?.data ?? response ?? []) as {
        id: string;
        nombreOficial: string;
      }[];
      const options = raw.map((locality) => ({
        label: locality.nombreOficial,
        value: locality.id,
      }));
      this.localityOptions.set(options);
    } catch (_error) {
      this.notification.error('Error al consultar las localidades');
    }
  }

  protected onSearchLocalitiesFilter(value: string | number): void {
    this.getLocalities(String(value));
  }

  async getPersonnelById() {
    try {
      this.isLoading.set(true);
      const data: PersonnelAdapted = await firstValueFrom(
        this.personnelService.findOne(this.id),
      );
      this.form.patchValue({
        personnelId: data.id,
        personId: data.person.id,
        organizationId: String(data.organizationId),
        positionId: data.positionId,
        positionNominateId: data.positionNominateId,
        localityId: data.localityId,
        isActive: data.isActive,
      });
      if (data.positionId) {
        const positionOptions = await this.getPositionsById(data.positionId);
        this.currentPositionOption.set({ value: positionOptions?.value || '', label: positionOptions?.label || '' });
      }
      if (data.positionNominateId) {
        const positionOptions = await this.getPositionsById(data.positionNominateId);
        this.currentPositionNominateOption.set({ value: positionOptions?.value || '', label: positionOptions?.label || '' });
      }
      if (data.localityId) {
        this.currentLocalityOption.set({
          value: data.localityId,
          label: data.localityName || data.localityId,
        });
      }
      this.foundPerson.set(data.person);
      this.loadPersonPhoto(data.person.id);
    } catch (_error) {
      this.notification.error('Error al consultar el personal');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onSearchPerson(identificationNumber: string) {
    if (!identificationNumber) {
      this.foundPerson.set(null);
      this.searchErrMsg.set('');
      this.form.get('personId')?.setValue('');
      this.photoControl.setValue(null);
      return;
    }
    try {
      const response = await firstValueFrom(
        this.personService.findByIdentificationNumber(identificationNumber),
      );
      this.foundPerson.set(response);
      this.form.get('personId')?.setValue(response.id);
      this.searchErrMsg.set('');
      this.loadPersonPhoto(response.id);
    } catch (_error) {
      this.foundPerson.set(null);
      this.form.get('personId')?.setValue('');
      this.searchErrMsg.set('Persona no encontrada');
      this.photoControl.setValue(null);
    }
  }

  private loadPersonPhoto(personId: string): void {
    this.photoControl.setValue(null);
    this.personService.getPhotobyPersonId(personId).subscribe({
      next: (blob) => this.photoControl.setValue(URL.createObjectURL(blob as Blob)),
      error: () => this.photoControl.setValue(null),
    });
  }

  private async uploadPhotoIfNeeded(): Promise<void> {
    const photo = this.photoControl.value;
    const personId = this.foundPerson()?.id;
    if (!(photo instanceof File) || !personId) return;

    try {
      this.isUploadingPhoto.set(true);
      await firstValueFrom(
        this.personService.updatePhotobyPersonId(personId, photo),
      );
    } catch (_error) {
      this.notification.error('Ocurrió un error al subir la foto.');
    } finally {
      this.isUploadingPhoto.set(false);
    }
  }

  get formData() {
    const raw = this.form.getRawValue();
    return {
      personnelId: raw.personnelId ?? '',
      personId: this.foundPerson()?.id ?? '',
      positionId: raw.positionId ?? '',
      positionNominateId: raw.positionNominateId || undefined,
      localityId: raw.localityId || undefined,
      organizationId: Number(raw.organizationId),
      isActive: raw.isActive ?? true,
    };
  }

  isFormValid(): boolean {
    if (this.form.invalid) {
      return false;
    }
    return true;
  }

  async onSubmit() {
    this.isSubmitted.set(true);
    this.form.markAllAsTouched();
    this.form.get('positionId')?.updateValueAndValidity();
    if (!this.isFormValid()) return;
 
    try {
      this.isPending.set(true);
      await firstValueFrom(this.personnelService.upsert(this.formData));
      await this.uploadPhotoIfNeeded();
      this.notification.success(
        this.isEdit
          ? 'Personal actualizado correctamente.'
          : 'Personal creado correctamente.',
      );
      this.router.navigate(['/personnel']);
    } catch (error) {
      this.notification.error(
        (error as Error).message ||
          `Ocurrió un error al ${this.isEdit ? 'actualizar' : 'crear'} el personal.`,
      );
    } finally {
      this.isPending.set(false);
    }
  }

  goBack() {
    this.router.navigate(['/personnel']);
  }
}
