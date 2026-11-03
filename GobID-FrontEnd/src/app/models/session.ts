export interface UserSession {
  sessionId: string;
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  identificationNumber: string | null;
  applicationId: string;
  ip: string;
  userAgent: string;
  isTrusted: boolean;
  isActive: boolean;
  createdAt: string;
  lastActivityAt: string;
  absoluteExpiresAt: string;
  revokedAt: string | null;
}

export interface SessionFilter {
  pageNumber?: number;
  pageSize?: number;
  IsActive?: string;
  ApplicationId?: string;
  search?: string;
}
