import { Component, inject, Input } from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  FormsModule,
  NgControl,
  ReactiveFormsModule,
} from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { TitleCasePipe } from '@angular/common';

@Component({
  selector: 'app-checkbox',
  templateUrl: './checkbox.html',
  imports: [ReactiveFormsModule, FormsModule, TitleCasePipe, FontAwesomeModule],
})
export class CheckboxComponent implements ControlValueAccessor {
  @Input() type = 'text';
  @Input() label = '';
  @Input() name = '';
  @Input() className = '';

  errors: string[] = [];

  public controlDir = inject(NgControl, { self: true });

  constructor() {
    this.controlDir.valueAccessor = this;
  }

  // Métodos requeridos para ControlValueAccessor
  writeValue(): void {
    return;
  }

  registerOnChange(): void {
    return;
  }

  registerOnTouched(): void {
    return;
  }

  get control(): FormControl {
    return this.controlDir.control as FormControl;
  }
}
