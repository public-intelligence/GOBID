export interface DecodedPayload {
  sub: string;
  exp: number;
  permissions: string;
  organization_name: string;
  organization_id: string;
  application_id: number;
  person_full_name: string;
  role_name: string;
  person_id: string;
}
