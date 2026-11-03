export interface AppCredential {
  appCredentialId: string;
  applicationId: string;
  description: string | null;
  isActive: boolean;
  createdAt: string;
  expiresAt: string;
  lastUsedAt: string | null;
  revokedAt: string | null;
  rotatedByCredentialId: string | null;
  isExpired: boolean;
}

export interface AppCredentialCreated extends AppCredential {
  clientSecret: string;
}

export interface CreateAppCredentialRequest {
  description?: string;
  expiryDays?: number;
  claimIds?: number[];
  organizationApplicationId?: number;
}

export type CredentialStatus = 'active' | 'expired' | 'revoked';

export function getCredentialStatus(cred: AppCredential): CredentialStatus {
  if (!cred.isActive) return 'revoked';
  if (cred.isExpired || new Date(cred.expiresAt) <= new Date()) return 'expired';
  return 'active';
}
