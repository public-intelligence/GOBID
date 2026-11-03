import { Component, inject, Input } from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  FormsModule,
  NgControl,
  ReactiveFormsModule,
} from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faChevronDown } from '@fortawesome/free-solid-svg-icons';
import { TitleCasePipe, NgClass } from '@angular/common';
import { HandleErrorsMessages } from '../utils';
import { ErrorLabelComponent } from '../error-label/error-label.component';

@Component({
  selector: 'app-textarea',
  templateUrl: './textarea.html',
  imports: [
    NgClass,
    ReactiveFormsModule,
    FormsModule,
    TitleCasePipe,
    FontAwesomeModule,
    ErrorLabelComponent,
  ],
})
export class TextAreaComponent implements ControlValueAccessor {
  @Input() type = 'text';
  @Input() label = '';
  @Input() name = '';
  @Input() placeholder = '';
  @Input() className = '';
  @Input() isSubmitted = false;
  @Input() errMsg = '';
  @Input() loading = false;

  errors: string[] = [];

  faChevronDown = faChevronDown;

  public controlDir = inject(NgControl, { self: true });

  constructor() {
    this.controlDir.valueAccessor = this;
  }

  onError = () => {
    return HandleErrorsMessages(this.control);
  };

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
