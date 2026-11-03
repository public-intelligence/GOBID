import { Select } from './select';

type filterType = 'select' | 'toggle' | 'search';

export interface Filter {
  title: string;
  values?: Select[];
  type: filterType;
  controlName: string;
  defaultValue: string | number | boolean;
}

export interface OrganizationFilter {
  pageNumber?: number;
  pageSize?: number;
  IsActive?: string;
  search?: string;
}

export interface ApplicationFilter {
  pageNumber?: number;
  pageSize?: number;
  IsActive?: string;
  search?: string;
  ApplicationId?: string;
  Code?: string;
}

export interface UserFilter {
  pageNumber?: number;
  pageSize?: number;
  IsActive?: string;
  search?: string;
  PersonnelId?: string;
  EmailLike?: string;
  IsServerAdmin?: string;
}

export interface OrgAppFilter {
  pageNumber?: number;
  pageSize?: number;
  organizationId?: number;
  search?: string;
}

export interface RolAppFilter {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}
