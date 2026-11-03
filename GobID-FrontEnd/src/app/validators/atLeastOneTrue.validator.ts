import {
  AbstractControl,
  ValidationErrors,
  AsyncValidatorFn,
  FormGroup,
} from '@angular/forms';

export function atLeastOneTrueValidator(): AsyncValidatorFn {
  return (formGroup: AbstractControl): Promise<ValidationErrors | null> => {
    if (!(formGroup instanceof FormGroup)) {
      return Promise.resolve(null);
    }
    const hasTrue = Object.values(formGroup.controls).some(
      (c) => c.value === true,
    );
    return Promise.resolve(hasTrue ? null : { atLeastOneTrue: true });
  };
}
