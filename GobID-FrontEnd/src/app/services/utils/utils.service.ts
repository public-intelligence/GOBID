import { inject, Injectable } from '@angular/core';
import { TokenDecoder } from '../token-decoder/token-decoder';

@Injectable({
  providedIn: 'root',
})
export class UtilsService {
  private tokenDecoder = inject(TokenDecoder);
  autoResize(id: string) {
    const element = document.getElementById(id) as HTMLTextAreaElement;
    if (element) {
      element.style.height = 'auto'; // Reset height to auto to calculate scrollHeight correctly
      element.style.height = element.scrollHeight + 'px'; // Set height to scrollHeight
    }
  }

  formatDate(date: string): string {
    const d = new Date(date);
    const day = String(d.getUTCDate()).padStart(2, '0');
    const month = String(d.getUTCMonth() + 1).padStart(2, '0');
    const year = d.getUTCFullYear();
    return `${day}-${month}-${year}`;
  }

  getMonth(fecha: Date | string): string {
    const meses = [
      'Enero',
      'Febrero',
      'Marzo',
      'Abril',
      'Mayo',
      'Junio',
      'Julio',
      'Agosto',
      'Septiembre',
      'Octubre',
      'Noviembre',
      'Diciembre',
    ];

    const date = typeof fecha === 'string' ? new Date(fecha) : fecha;

    if (isNaN(date.getTime())) {
      return '';
    }

    return meses[date.getUTCMonth()];
  }

  /**
   * Retorna un valor seguro (un string vacio), evitando null o undefined.
   */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  safe(value: any) {
    return value === undefined || value === null ? '' : value;
  }

  hasPermission(permissions: string[]): boolean {
    const userPermissions = this.tokenDecoder.getPermissions();
    return permissions.some((permission) =>
      userPermissions.includes(permission),
    );
  }

  toUpperIfLetter(value: string): string {
    return /^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ]$/.test(value) ? value.toUpperCase() : value;
  }
}
