import { ClaimCreate } from "./claim";

export interface Application {
  applicationId: number;
  applicationName: string;
  code: string;
  redirectUrl: string;
  applicationDescription: string;
  isActive: boolean;
  isPublic: boolean;
  createdBy: string | null;
  createdAt: string;
  updatedBy: string | null;
  updatedAt: string | null;
}

export interface ApplicationAdapted {
  id: number;
  name: string;
  code: string;
  redirectUrl: string;
  description: string;
  isActive: boolean;
  isPublic: boolean;
  createdBy: string | null;
  createdAt: string;
  updatedBy: string | null;
  updatedAt: string | null;
}

export interface ApplicationCreate {
  applicationId: number;
  applicationName: string;
  code: string;
  redirectUrl: string;
  applicationDescription: string;
  isActive?: boolean;
  isPublic?: boolean;
  claims?: ClaimCreate[]
}

export interface ApplicationCreateResponse {
  data: {
    applicationId: number;
  };
}
