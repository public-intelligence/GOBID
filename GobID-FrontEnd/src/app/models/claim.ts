export interface Claim {
  claimId: number;
  applicationId: number;
  code: string;
  claimName: string;
  claimDescription: string;
  isActive: boolean;
  isAutomatic: boolean;
  createdBy: string | null;
  createdAt: string;
  updatedBy: string | null;
  updatedAt: string | null;
}

export interface ClaimAdapted {
  id: number;
  applicationId: number;
  code: string;
  name: string;
  description: string;
  isActive: boolean;
  isAutomatic: boolean;
  createdBy: string | null;
  createdAt: string;
  updatedBy: string | null;
  updatedAt: string | null;
}

export interface ClaimCreate {
  claimId: number;
  applicationId: number | null;
  code: string;
  claimName: string;
  claimDescription: string;
  isActive: boolean;
  isAutomatic: boolean
}
