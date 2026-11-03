import { AbstractControl } from '@angular/forms';

export function noDefaultValidator(control: AbstractControl) {
  return control.value !== 'default' ? null : { default: true };
}
