export interface PolicyType {
  policyTypeId: number;
  name: 'IP_RANGE' | 'TIME_WINDOW';
  description?: string;
  isActive: boolean;
  createdAt?: string;
  createdBy?: string;
}

export interface PolicyTypeAdapted {
  policyTypeId: number;
  name: 'IP_RANGE' | 'TIME_WINDOW';
  description?: string;
  isActive: boolean;
  createdAt?: string;
  createdBy?: string;
}

export interface Policy {
  policyId: string;
  organizationId: number;
  policyTypeId: number;
  policyTypeName?: string;
  name: string;
  description?: string;
  effect: 'ALLOW' | 'DENY';
  isActive: boolean;
  createdAt?: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
  rowVersion?: string;
  conditions: PolicyCondition[];
}

export interface PolicyAdapted {
  policyId: string;
  organizationId: number;
  policyTypeId: number;
  policyTypeName?: string;
  name: string;
  description?: string;
  effect: 'ALLOW' | 'DENY';
  isActive: boolean;
  createdAt?: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
  rowVersion?: string;
  conditions?: PolicyConditionAdapted[];
  conditionFormatted?: Record<string, string>;
}

export interface PolicyCondition {
  policyConditionId: number;
  policyId: string;
  conditionKey:
    | 'IpRangeStart'
    | 'IpRangeEnd'
    | 'StartTime'
    | 'EndTime'
    | 'Timezone';
  conditionValue: string;
}

export interface PolicyConditionAdapted {
  policyConditionId: number;
  policyId: string;
  conditionKey:
    | 'IpRangeStart'
    | 'IpRangeEnd'
    | 'StartTime'
    | 'EndTime'
    | 'Timezone';
  conditionValue: string;
}

export interface UserApplicationPolicy {
  userApplicationPolicyId: string;
  userApplicationId: string;
  policyId: string;
  isActive: boolean;
  createdAt?: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}

export interface UserApplicationPolicyAdapted {
  userApplicationPolicyId: string;
  userApplicationId: string;
  policyId: string;
  isActive: boolean;
  createdAt?: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
}

export interface UserApplicationPolicyCreate {
  userApplicationId: string;
  policyId: string;
  isActive: boolean;
}

export interface PolicyTypeFilter {
  pageNumber?: number;
  pageSize?: number;
  IsActive?: string;
  search?: string;
}

export interface PolicyFilter {
  pageNumber?: number;
  pageSize?: number;
  OrganizationId?: number;
  PolicyTypeId?: number;
  Effect?: 'ALLOW' | 'DENY';
  IsActive?: string;
  search?: string;
}

export interface PolicyConditionFilter {
  pageNumber?: number;
  pageSize?: number;
  PolicyId?: string;
  ConditionKey?: string;
}

export interface UserApplicationPolicyFilter {
  pageNumber?: number;
  pageSize?: number;
  UserApplicationId?: string;
  PolicyId?: string;
  IsActive?: string;
}

export interface PolicyEvaluationLogFilter {
  pageNumber?: number;
  pageSize?: number;
  UserId?: string;
  PolicyId?: string;
  OrganizationApplicationId?: number;
  EvaluationResult?: 'ALLOWED' | 'DENIED';
  EvaluatedAtFrom?: string;
  EvaluatedAtTo?: string;
}
