import { AbstractControl, ValidationErrors } from '@angular/forms';

export function samePositionValidator(control: AbstractControl): ValidationErrors | null {
  const parent = control.parent;
  if (!parent) return null;

  const positionNominateId = parent.get('positionNominateId')?.value;
  if (!control.value || !positionNominateId) return null;

  return String(control.value) === String(positionNominateId) ? { samePosition: true } : null;
}
