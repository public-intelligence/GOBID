import {
  Component,
  inject,
  signal,
  OnInit,
  ChangeDetectorRef,
  ViewChild,
} from '@angular/core';
import {
  InputComponent,
  TextAreaComponent,
  CheckboxComponent,
  SearchComponent,
} from '../../../components/shared';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { LayoutService } from '@public-intelligence/ui-core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faPlus,
  faArrowLeft,
  faTrash,
} from '@fortawesome/free-solid-svg-icons';
import { ActivatedRoute, Router } from '@angular/router';
import { OrganizationService } from '../../../services/organization/organization.service';
import {
  Organization,
  OrganizationAdapted,
  OrganizationContactAdapted,
  OrganizationCreate,
} from '../../../models/organization';
import { firstValueFrom } from 'rxjs/internal/firstValueFrom';
import {
  LegalEntityService,
  NotificationService,
  OrganizationContactService,
} from '../../../services';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import { LegalEntityAdapted, ServerResponse } from '../../../models';

@Component({
  selector: 'app-organization-maintenance',
  imports: [
    InputComponent,
    ReactiveFormsModule,
    FontAwesomeModule,
    TextAreaComponent,
    CheckboxComponent,
    ConfirmationModalComponent,
    SearchComponent,
  ],
  templateUrl: './organization-maintenance.html',
})
export class OrganizationMaintenanceComponent implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  private fb = inject(FormBuilder);
  private organizationService = inject(OrganizationService);
  private legalEntityService = inject(LegalEntityService);
  private organizationContactService = inject(OrganizationContactService);
  private notification = inject(NotificationService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private contactCount = 0;
  private cdr = inject(ChangeDetectorRef);

  protected legalEntityFound = signal<LegalEntityAdapted | null>(null);
  protected isSearchingLegalEntity = signal(false);

  protected id = this.route.snapshot.paramMap.get('id');
  protected idApp = this.route.snapshot.paramMap.get('idApp');
  protected organizationFound: OrganizationAdapted | null = null;
  protected faPlus = faPlus;
  protected faArrowLeft = faArrowLeft;
  protected faTrash = faTrash;
  protected submitted = signal(false);
  protected isLoading = signal(false);
  protected isPending = signal(false);

  private layoutService = inject(LayoutService);

  protected organizationForm = this.fb.group({
    legalEntityId: ['', Validators.required],
    name: ['', Validators.required],
    acronym: ['', Validators.required],
    slogan: [''],
    website: ['', Validators.required],
    organizationAddress: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
    description: ['', Validators.required],
    organizationContacts: this.fb.array([]),
    isActive: [true],
  });

  get organizationContacts(): FormArray {
    return this.organizationForm.get('organizationContacts') as FormArray;
  }

  async ngOnInit() {
    this.layoutService.setPageTitle('Mantenimiento de Organización');
    this.layoutService.breadcrumbs.set([
      { label: 'Organizaciones', url: '/organizations' },
      {
        label: this.id ? 'Editar Organización' : 'Registrar Organización',
        url: '/organizations/create',
      },
    ]);
    if (this.id) {
      await this.findOrganizationById(this.id);
    } else {
      this.addContact();
    }
    this.cdr.detectChanges();
  }

  /**
   *  Add contacts to the form
   */
  setContact(contact?: OrganizationContactAdapted) {
    this.organizationContacts.push(
      this.fb.group({
        contactId: [
          contact?.contactId || this.contactCount,
          Validators.required,
        ],
        organizationId: [this.id || 0],
        organizationContactName: [
          contact?.organizationContactName || '',
          Validators.required,
        ],
        position: [contact?.position || '', Validators.required],
        email: [contact?.email || '', Validators.required],
        phone: [contact?.phone || '', Validators.required],
        isActive: [contact?.isActive || false, Validators.required],
        isNew: [!contact],
      }),
    );

    this.contactCount++;
  }

  addContact() {
    if (this.organizationContacts.length >= 5) {
      this.notification.error('No se pueden agregar más de 5 contactos');
      return;
    }
    this.setContact();
  }

  /**
   *  Remove a contact from the form
   */
  removeContact(index: number) {
    if (this.organizationContacts.length === 1) return;
    this.organizationContacts.removeAt(index);
  }

  /**
   * Send The form's info to the api
   */
  onSubmit = async () => {
    this.submitted.set(true);
    if (this.organizationForm.invalid) return;
    try {
      this.isPending.set(true);

      const org = await this.upsertOrganization();
      await this.upsertOrganizationContacts(org.organizationId);
      this.notification.onToast({
        message: !this.id
          ? 'Se ha guardado la información de la organización correctamente'
          : 'Se ha actualizado la información de la organización correctamente',
        status: 'success',
      });
      this.goBack();
    } catch (error) {
      this.notification.error(
        (error as Error).message ||
          (this.id
            ? 'Error al Registrar la información de la organización'
            : 'Error al guardar la información de la organización'),
      );
    } finally {
      this.isPending.set(false);
    }
  };

  /**
   *  Find an organization by ID
   * @param id ID of the organization to be found
   */
  async findOrganizationById(id: string) {
    try {
      this.isLoading.set(true);
      const response = await firstValueFrom(
        this.organizationService.findOne(id),
      );
      this.onSetValues(response);
      if (response.legalEntityId) {
        await this.loadLegalEntityById(response.legalEntityId);
      }
    } catch (_error) {
      this.notification.error('Error al consultar las organizaciones');
    } finally {
      this.isLoading.set(false);
    }
  }

  /**
   * Load legal entity info for display (edit mode)
   */
  async loadLegalEntityById(legalEntityId: string) {
    try {
      const legalEntity = await firstValueFrom(
        this.legalEntityService.findOne(legalEntityId),
      );
      this.legalEntityFound.set(legalEntity as unknown as LegalEntityAdapted);
    } catch (_error) {
      // fail silently — legalEntityId is already in the form
    }
  }

  /**
   * Search for a legal entity by identification number (RNC)
   */
  async getLegalEntityByIdentificationNumber(identificationNumber: string) {
    try {
      this.isSearchingLegalEntity.set(true);
      const response = await firstValueFrom(
        this.legalEntityService.findByIdentificationNumber(
          identificationNumber,
        ),
      );
      this.legalEntityFound.set(response as unknown as LegalEntityAdapted);
      this.organizationForm.patchValue({
        legalEntityId: (response as unknown as LegalEntityAdapted).id,
      });
    } catch (_error) {
      this.notification.error(
        'No se encontró ninguna entidad legal con ese número de identificación',
      );
      this.legalEntityFound.set(null);
      this.organizationForm.patchValue({ legalEntityId: '' });
    } finally {
      this.isSearchingLegalEntity.set(false);
    }
  }

  onSearchLegalEntity(identificationNumber: string) {
    if (!identificationNumber) {
      this.legalEntityFound.set(null);
      this.organizationForm.patchValue({ legalEntityId: '' });
      return;
    }
    this.getLegalEntityByIdentificationNumber(identificationNumber);
  }

  /**
   * Register or edit an Organization
   */
  async upsertOrganization() {
    const organization: OrganizationCreate = this.onGetValues();
    const response = (await firstValueFrom(
      this.organizationService.upsert(organization),
    )) as ServerResponse<Organization>;

    return response.data;
  }

  /**
   * Register or edit the organization's contacts
   */
  async upsertOrganizationContacts(id: number) {
    let organizationContacts = this.onGetOrganizationContactsValues();
    if (!this.id) {
      organizationContacts = organizationContacts.map((item) => {
        return {
          ...item,
          organizationId: id,
        };
      });
    }
    await firstValueFrom(
      this.organizationContactService.upsert(organizationContacts),
    );
  }

  /**
   *  Set the form values with the organization data
   * @param organization Organization data to set in the form
   */
  onSetValues(organization: OrganizationAdapted) {
    organization.contact?.map((contact) => this.setContact(contact));
    this.organizationForm.patchValue({
      legalEntityId: organization.legalEntityId,
      name: organization.name,
      acronym: organization.acronym,
      slogan: organization.slogan,
      website: organization.website,
      organizationAddress: organization.organizationAddress,
      email: organization.email,
      phone: organization.phone,
      description: organization.description,
      isActive: organization.isActive,
    });
  }

  /**
   *  Get the form's values
   */
  onGetValues(): OrganizationCreate {
    const organization = {
      legalEntityId: this.organizationForm.value.legalEntityId ?? '',
      organizationId: this.id ? Number(this.id) : undefined,
      organizationName: this.organizationForm.value.name ?? '',
      acronym: this.organizationForm.value.acronym ?? '',
      slogan: this.organizationForm.value.slogan ?? '',
      website: this.organizationForm.value.website ?? '',
      organizationAddress:
        this.organizationForm.value.organizationAddress ?? '',
      email: this.organizationForm.value.email ?? '',
      phone: this.organizationForm.value.phone ?? '',
      organizationDescription: this.organizationForm.value.description ?? '',
      isActive: true,
    };

    return organization;
  }

  /**
   *  Get organization contacts values from form
   */
  onGetOrganizationContactsValues(): OrganizationContactAdapted[] {
    const organizationContact =
      this.organizationForm.value.organizationContacts ?? [];
    return organizationContact as OrganizationContactAdapted[];
  }

  /**
   *  Delete a contact from databases if this exist
   * @param id  contact id
   * @param index contact position
   */
  async onDeleteContact(id: number, index: number) {
    try {
      await firstValueFrom(
        this.organizationContactService.delete(id.toString()),
      );
      this.notification.onToast({
        message: 'Contacto eliminado correctamente',
        status: 'success',
      });
      this.removeContact(index);
      this.cdr.markForCheck();
    } catch (_error) {
      this.notification.error(
        'Error al eliminar el contacto de la organización',
      );
    }
  }

  /**
   Modal to confirm if the user wants to delete the contact selected
   */
  showDeleteConfirmationContact(id: number, name: string, index: number) {
    if (this.organizationContacts.length === 1) {
      this.notification.error(
        'No es posible eliminar este contacto, ya que debe haber al menos uno registrado',
      );
      return;
    }
    this.confirmationModal.openModal({
      isDelete: true,
      title: 'Eliminar Contacto',
      name: name.toUpperCase(),
      paragraph: `Está seguro(a) que desea eliminar el contacto de`,
      confirmButton: 'Eliminar',
      onConfirm: () => {
        this.onDeleteContact(id, index);
      },
    });
  }

  /**
   * Manage the type of delete to be done
   */
  deleteType(index: number) {
    if (this.organizationContacts.at(index).get('isNew')?.value) {
      this.removeContact(index);
    } else {
      this.showDeleteConfirmationContact(
        this.organizationContacts.at(index).get('contactId')?.value,
        this.organizationContacts.at(index).get('organizationContactName')
          ?.value,
        index,
      );
    }
  }

  /**
   *  Navigate back to the organizations list
   */
  goBack() {
    this.router.navigate(['/organizations']);
  }
}
