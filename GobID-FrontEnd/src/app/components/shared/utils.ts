import { FormControl } from '@angular/forms';

export function HandleErrorsMessages(control: FormControl): string[] {
  const errorKeys = Object.keys(control?.errors || '');
  return errorKeys.map((key) => {
    switch (key) {
      case 'minimumAge':
        return 'La fecha seleccionada no es válida';
      case 'minlength':
        return `Se requiere de un mínimo de ${control.getError('minlength')?.requiredLength} caracteres`;
      case 'maxlength':
        return `Se requiere de un máximo de ${control.getError('maxlength')?.requiredLength} caracteres`;
      case 'required':
        return 'Este campo es requirido';
      case 'passwordStrength':
        return 'La contraseña debe contener al menos 8 caracteres, una letra mayúscula, una letra minúscula y un número';
      case 'passwordMismatch':
        return 'Las contraseñas no coinciden';
      case 'default':
        return 'Debes seleccionar una opción';
      case 'server':
        return control.getError('server');
      case 'atLeastOneTrue':
        return 'Debes seleccionar al menos una opción';
      case 'invalidFileType':
        return 'El archivo seleccionado debe ser una imagen';
      case 'fileTooLarge':
        return `La imagen no debe superar los ${control.getError('fileTooLarge')?.maxSizeMB}MB`;
      default:
        return 'Campo inválido';
    }
  });
}
