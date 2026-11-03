export interface UserOrgApp {
  organizationApplicationId: number;
  organizationId: number;
  organizationName: string;
  applicationId: string;
  applicationName: string;
  policyAccess: boolean;
  policyMessage: string;
}

export interface AuthRequest {
  login: string;
  password: string;
  applicationId: string;
  isTrustedDevice: boolean;
}

export interface AuthResponse {
  code: string;
  userOrgApps: UserOrgApp[];
  mustChangePassword: boolean;
  passwordResetToken: string;
}

export interface AuthTokenRequest {
  code: string;
  redirectUri: string;
}

export interface AuthTokenResponse {
  accessToken: string;
  mustChangePassword: boolean;
}

export interface RefreshTokenResponse {
  accessToken: string;
}

export interface ChangePassword {
  newPassword: string;
  confirmNewPassword: string;
}

export interface ResetPassword {
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ForgotPassword {
  email: string;
  applicationId: string;
}

export interface RegisterUser {
  email: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
  personnelId: string;
}

export interface RegisterUserResponse {
  succeeded: boolean;
  message: string;
  data: string;
}
