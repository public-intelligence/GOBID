import { Locality } from "./locality";
import { Person } from "./person";


export interface Personnel {
  personnelId: string;
  positionId: string;
  position : Position;
  positionNominateId?: string;
  localityId?: string;
  organizationId: number;
  person : Person
  locality?: Locality;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface PersonnelAdapted {
  id: string;
  personnelId: string;
  positionId: string;
  positionName: string;
  positionNominateId: string;
  localityId: string;
  localityName: string;
  person : Person
  organizationId: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface PersonnelUpsert {
  personnelId?: string;
  positionId: string;
  positionNominateId?: string;
  localityId?: string;
  organizationId: number;
  isActive: boolean;
}

export interface PersonnelFilter {
  isActive?: string;
  pageNumber?: number;
  pageSize?: number;
  positionId?: string;
  searchTerm?: string;
}

export interface Position {
  id: string;
  name: string;
  grade: number;
  code: number;
  isManager: boolean;
  competencies?: string;
}

export interface PositionAdapted {
  id: string;
  name: string;
  grade: number;
  code: number;
  isManager: boolean;
}
