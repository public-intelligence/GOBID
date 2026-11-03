import { Position } from "./position.model";

export interface Person {
  id: string;
  firstName: string;
  lastName: string;
  identificationNumber: string;
  gender: string;
  email: string;
  phoneNumber: string;
  isExternal: boolean;
  status: string;
  position ?: Position;
  organizationalStructure ?: Structure;
  organizationId?: string;
  personnelId?: string;
}

export interface PersonAdapted {
  id: string;
  firstName: string;
  lastName: string;
  identificationNumber: string;
  gender: string;
  organizationId?: string;
  email: string;
  phoneNumber: string;
  isExternal: boolean;
  status: string;
  personnelId?: string;
  position?: Position;
  positionId?: string;
  organizationalStructure?:Structure;
  
}

export interface PersonCreate {
  id?: string;
  firstName: string;
  lastName: string;
  identificationNumber: string;
  gender: string;
  email: string;
  phoneNumber: string;
  isExternal: boolean;
  birthdate: string;
  status: string;
}

export interface PersonUserCreate {
  id?: string;
  firstName: string;
  lastName: string;
  identificationNumber: string;
  gender: string;
  email: string;
  phoneNumber: string;
  isExternal: boolean;
  birthdate: string;
  status: string;
  password: string;
  confirmPassword: string;
}

export interface PersonCreateResponse {
  data: {
    id: string;
  };
}

export interface PersonFilter {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  status?: string;
}
