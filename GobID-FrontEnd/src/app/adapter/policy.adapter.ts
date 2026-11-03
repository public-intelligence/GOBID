import { inject, Injectable } from '@angular/core';
import {
  Policy,
  PolicyType,
  PolicyCondition,
  UserApplicationPolicy,
  PolicyTypeAdapted,
  PolicyAdapted,
  PolicyConditionAdapted,
  UserApplicationPolicyAdapted,
} from '../models/policy';

@Injectable({
  providedIn: 'root',
})
export class PolicyTypeAdapter {
  adapt(apiData: PolicyType): PolicyTypeAdapted {
    return {
      policyTypeId: apiData.policyTypeId,
      name: apiData.name,
      description: apiData.description ?? '',
      isActive: apiData.isActive ?? true,
      createdAt: apiData.createdAt ?? '',
      createdBy: apiData.createdBy ?? '',
    };
  }
}

@Injectable({
  providedIn: 'root',
})
export class PolicyAdapter {
  private policyConditionAdapter = inject(PolicyConditionAdapter);

  adapt(apiData: Policy): PolicyAdapted {
    return {
      policyId: apiData.policyId ?? '',
      organizationId: apiData.organizationId ?? 0,
      policyTypeId: apiData.policyTypeId ?? 0,
      policyTypeName: apiData.policyTypeName ?? '',
      name: apiData.name ?? '',
      description: apiData.description ?? '',
      effect: apiData.effect,
      isActive: apiData.isActive ?? true,
      createdAt: apiData.createdAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedAt: apiData.updatedAt ?? '',
      updatedBy: apiData.updatedBy ?? '',
      rowVersion: apiData.rowVersion ?? '',
      conditions: apiData.conditions.map((condition) =>
        this.policyConditionAdapter.adapt(condition),
      ),
    };
  }
}

@Injectable({
  providedIn: 'root',
})
export class PolicyConditionAdapter {
  adapt(apiData: PolicyCondition): PolicyConditionAdapted {
    return {
      policyConditionId: apiData.policyConditionId ?? 0,
      policyId: apiData.policyId ?? '',
      conditionKey: apiData.conditionKey,
      conditionValue: apiData.conditionValue ?? '',
    };
  }
}

@Injectable({
  providedIn: 'root',
})
export class UserApplicationPolicyAdapter {
  adapt(apiData: UserApplicationPolicy): UserApplicationPolicyAdapted {
    return {
      userApplicationPolicyId: apiData.userApplicationPolicyId ?? '',
      userApplicationId: apiData.userApplicationId ?? '',
      policyId: apiData.policyId ?? '',
      isActive: apiData.isActive ?? true,
      createdAt: apiData.createdAt ?? '',
      createdBy: apiData.createdBy ?? '',
      updatedAt: apiData.updatedAt ?? '',
      updatedBy: apiData.updatedBy ?? '',
    };
  }
}
