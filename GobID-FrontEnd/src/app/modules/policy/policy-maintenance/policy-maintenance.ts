import {
  ChangeDetectorRef,
  Component,
  inject,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import {
  FormArray,
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { faArrowLeft } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';

import { NotificationService, TokenDecoder } from '../../../services';
import { PolicyService } from '../../../services/policy/policy.service';
import { PolicyTypeService } from '../../../services/policy/policy-type.service';
import {
  PolicyAdapted,
  PolicyConditionAdapted,
  PolicyTypeAdapted,
} from '../../../models/policy';
import { Select } from '../../../models';
import {
  InputComponent,
  TextAreaComponent,
  CheckboxComponent,
  SelectComponent,
} from '../../../components/shared';
import { ConfirmationModalComponent } from '../../../components/ui/modals/confirmation-modal/confirmation-modal';
import {
  ConditionKeyConfig,
  CONDITION_KEYS_BY_TYPE,
  EFFECT_OPTIONS,
  TIMEZONE_OPTIONS,
  APPLY_ON_DAYS,
} from './config';
import { LayoutService } from '@public-intelligence/ui-core';

@Component({
  selector: 'app-policy-maintenance',
  imports: [
    ReactiveFormsModule,
    FontAwesomeModule,
    InputComponent,
    TextAreaComponent,
    CheckboxComponent,
    SelectComponent,
    ConfirmationModalComponent,
  ],
  templateUrl: './policy-maintenance.html',
})
export class PolicyMaintenance implements OnInit {
  @ViewChild(ConfirmationModalComponent)
  confirmationModal!: ConfirmationModalComponent;

  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private policyService = inject(PolicyService);
  private policyTypeService = inject(PolicyTypeService);
  private notification = inject(NotificationService);
  private tokenDecoder = inject(TokenDecoder);
  private cdr = inject(ChangeDetectorRef);
  private counter = 0;

  private policyTypes = signal<PolicyTypeAdapted[]>([]);

  protected id = this.route.snapshot.paramMap.get('id');
  protected faArrowLeft = faArrowLeft;

  private layoutService = inject(LayoutService);
  protected submitted = signal(false);
  protected isLoading = signal(false);
  protected isPending = signal(false);
  protected policyTypeOptions = signal<Select[]>([]);
  protected effectOptions = EFFECT_OPTIONS;
  protected currentTypeKeys = signal<ConditionKeyConfig[]>([]);
  protected timezoneOptions = TIMEZONE_OPTIONS;
  protected readonly applyOnDays = APPLY_ON_DAYS;

  protected policyForm = this.fb.group({
    name: ['', Validators.required],
    description: [''],
    policyTypeId: ['', Validators.required],
    effect: ['ALLOW', Validators.required],
    isActive: [true],
    conditions: this.fb.array([]),
  });

  get conditions(): FormArray {
    return this.policyForm.get('conditions') as FormArray;
  }

  async ngOnInit() {
    this.layoutService.setPageTitle('Mantenimiento de Política');
    this.layoutService.breadcrumbs.set([
      { label: 'Políticas', url: '/policy' },
      { label: 'Mantenimiento', url: 'policy/create' },
    ]);
    await this.loadPolicyTypes();

    this.policyForm.get('policyTypeId')!.valueChanges.subscribe((typeId) => {
      if (!typeId) {
        this.conditions.clear();
        this.currentTypeKeys.set([]);
        return;
      }
      const type = this.policyTypes().find(
        (t) => t.policyTypeId.toString() === typeId,
      );
      if (type) this.buildConditionsFromType(type.name);
    });

    if (this.id) {
      await this.loadPolicy(this.id);
    }
    this.cdr.detectChanges();
  }

  private async loadPolicyTypes() {
    try {
      const res = await firstValueFrom(this.policyTypeService.findAll());
      this.policyTypes.set(res.data as unknown as PolicyTypeAdapted[]);
      this.policyTypeOptions.update((options) => [
        {
          label: 'Seleccione un tipo de política',
          value: '',
        },
        ...(res.data as unknown as PolicyTypeAdapted[]).map((t) => ({
          label: t.name,
          value: t.policyTypeId.toString(),
        })),
      ]);
    } catch (_error) {
      this.notification.error('Error al cargar los tipos de política');
    }
  }

  private async loadPolicy(id: string) {
    try {
      this.isLoading.set(true);
      const policy = (await firstValueFrom(
        this.policyService.findOne(id),
      )) as PolicyAdapted;
      this.patchForm(policy, policy.conditions ?? []);
    } catch (_error) {
      this.notification.error('Error al cargar la política');
    } finally {
      this.isLoading.set(false);
    }
  }

  private patchForm(
    policy: PolicyAdapted,
    conditions: PolicyConditionAdapted[],
  ) {
    this.policyForm.patchValue(
      {
        name: policy.name,
        description: policy.description ?? '',
        policyTypeId: policy.policyTypeId.toString(),
        effect: policy.effect,
        isActive: policy.isActive,
      },
      { emitEvent: false },
    );

    const type = this.policyTypes().find(
      (t) => t.policyTypeId === policy.policyTypeId,
    );
    if (type) this.buildConditionsFromType(type.name, conditions);
  }

  private buildConditionsFromType(
    typeName: string,
    existing: PolicyConditionAdapted[] = [],
  ) {
    this.conditions.clear();
    const keys = CONDITION_KEYS_BY_TYPE[typeName] ?? [];
    this.currentTypeKeys.set(keys);
    keys.forEach((key) => {
      const match = existing.find((c) => c.conditionKey === key.value);
      this.conditions.push(
        this.fb.group({
          policyConditionId: [match?.policyConditionId ?? this.counter],
          policyId: [this.id ?? ''],
          conditionKey: [key.value, Validators.required],
          conditionValue: [match?.conditionValue ?? '', Validators.required],
        }),
      );
      this.counter++;
    });
  }

  onSubmit = async () => {
    this.submitted.set(true);
    if (this.policyForm.invalid) return;
    try {
      this.isPending.set(true);
      const organizationId = Number(this.tokenDecoder.getOrganizationId());
      const policyPayload: PolicyAdapted = {
        policyId: this.id ?? '',
        organizationId,
        policyTypeId: Number(this.policyForm.value.policyTypeId),
        name: this.policyForm.value.name!,
        description: this.policyForm.value.description ?? '',
        effect: this.policyForm.value.effect as 'ALLOW' | 'DENY',
        isActive: this.policyForm.value.isActive ?? true,
        conditions: this.policyForm.value
          .conditions as PolicyConditionAdapted[],
      };

      await firstValueFrom(this.policyService.upsert(policyPayload));

      this.notification.success(
        this.id
          ? 'Política actualizada correctamente'
          : 'Política creada correctamente',
      );
      this.goBack();
    } catch (error) {
      this.notification.error(
        (error as Error)?.message ?? 'Error al guardar la política',
      );
    } finally {
      this.isPending.set(false);
    }
  };

  isDaySelected(conditionIndex: number, day: string): boolean {
    const val =
      this.conditions.at(conditionIndex).get('conditionValue')?.value ?? '';
    return val.split(',').filter(Boolean).includes(day);
  }

  toggleDay(conditionIndex: number, day: string): void {
    const ctrl = this.conditions.at(conditionIndex).get('conditionValue');
    const current: string[] = (ctrl?.value ?? '').split(',').filter(Boolean);
    const idx = current.indexOf(day);
    if (idx >= 0) {
      current.splice(idx, 1);
    } else {
      current.push(day);
    }
    ctrl?.setValue(current.join(','));
    ctrl?.markAsTouched();
  }

  goBack() {
    this.router.navigate(['/policy']);
  }
}
