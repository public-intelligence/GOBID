import { LegalEntity, LegalEntityAdapted } from "./legal-entity";

export interface Organization {
  organizationId: number;
  legalEntityId: string;
  organizationName: string;
  acronym: string;
  organizationDescription: string;
  slogan: string;
  website: string;
  email: string;
  organizationAddress: string;
  phone: string;
  contact?: OrganizationContact[];
  isActive: boolean;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
  createdBy: string | null;
  updatedBy: string | null;
  count: number;
  legalEntity?: LegalEntity;
}

export interface OrganizationAdapted {
  id: number;
  legalEntityId: string;
  name: string;
  acronym: string;
  description: string;
  slogan: string;
  website: string;
  email: string;
  phone: string;
  organizationAddress: string;
  contact?: OrganizationContactAdapted[];
  isActive: boolean;
  rowVersion: string;
  createdAt: string;
  updatedAt: string | null;
  createdBy: string | null;
  updatedBy: string | null;
  count: number;
  legalEntity?: LegalEntityAdapted;
}

export interface OrganizationCreate {
  legalEntityId: string;
  organizationId?: number;
  organizationName: string;
  acronym: string;
  organizationDescription: string;
  slogan: string;
  website: string;
  email: string;
  organizationAddress: string;
  phone: string;
  isActive: boolean;
}

export interface OrganizationContact {
  contactId: number;
  organizationId: number;
  organizationContactName: string;
  position: string;
  email: string;
  phone: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface OrganizationContactAdapted {
  contactId: number;
  organizationId: number;
  organizationContactName: string;
  position: string;
  email: string;
  phone: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface OrganizationContactUpsert {
  contactId?: number;
  organizationId: number;
  organizationContactName: string;
  position: string;
  email: string;
  phone: string;
  isActive: boolean;
}
