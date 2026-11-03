import { Injectable } from '@angular/core';
import { Notification, StatusNotification } from '../../models/notification';
import Toastify from 'toastify-js';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private duration = 4000;
  private errorColor = 'rgb(255, 50, 54)';
  private successColor = 'rgb(46, 204, 113)';
  private warningColor = 'rgb(211, 189, 18)';
  private defaultColor = 'rgb(49, 110, 197)';
  private style = {
    background: 'white',
    padding: '1.3rem 1.5rem',
    borderLeft: '8px solid rgb(41, 87, 238)',
    cursor: 'default',
    fontWeight: '600',
  };

  private toast = {
    text: '',
    style: { ...this.style },
    position: 'center' as const,
    duration: this.duration,
  };

  /**
    Renderiza una notificación en pantalla.
    @param message - El mensaje que se quiere emitir.
    @param duration - La cantidad de milisegundos que debe durar en pantalla el mensaje.
    @param status - El estado que determina el color de la notificación.
   */
  onToast({ message, duration, status }: Notification) {
    Toastify({
      ...this.toast,
      text: message,
      style: {
        ...this.style,
        color: `${this.selectColor(status)}`,
        borderLeft: `8px solid ${this.selectColor(status)}`,
      },
      duration: duration ? duration : this.duration,
    }).showToast();
  }

  success(message: string, duration?: number) {
    this.onToast({
      message,
      duration,
      status: 'success',
    });
  }

  error(message: string, duration?: number) {
    this.onToast({
      message,
      duration,
      status: 'error',
    });
  }

  /**
   * Retorna el rgb del color a utilizar
   * @param status - Estado que determina el color de la notificación
   * @returns - Retorna un rgb
   */
  private selectColor(status: StatusNotification) {
    switch (status) {
      case 'success':
        return this.successColor;
      case 'error':
        return this.errorColor;
      case 'warning':
        return this.warningColor;
      default:
        return this.defaultColor;
    }
  }
}
