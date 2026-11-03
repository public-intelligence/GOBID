import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';
import { DecodedPayload } from '../../models';

@Injectable({
  providedIn: 'root',
})
export class TokenDecoder {
  private decodeToken(): DecodedPayload | null {
    try {
      const decoded: DecodedPayload = jwtDecode(this.getAccessToken());
      return decoded;
    } catch (_error) {
      return null;
    }
  }

  getPermissions(): string[] {
    const permissions = JSON.parse(this.decodeToken()?.permissions || '[]');
    return permissions;
  }

  getOrganizationName() {
    return this.decodeToken()?.organization_name || '';
  }

  getApplicationId() {
    return this.decodeToken()?.application_id || '';
  }

  getOrganizationId() {
    return this.decodeToken()?.organization_id || '';
  }

  getPersonFullName() {
    return this.decodeToken()?.person_full_name || '';
  }

  getRole() {
    return this.decodeToken()?.role_name || '';
  }

  getPersonId() {
    return this.decodeToken()?.person_id || '';
  }

  getAccessToken() {
    return sessionStorage.getItem('accessToken') || '';
  }
  
}
