import { NgClass, TitleCasePipe } from '@angular/common';
import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  FormsModule,
  NgControl,
  ReactiveFormsModule,
} from '@angular/forms';
import { Select } from '../../../models/select';
import { HandleErrorsMessages } from '../utils';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faChevronDown } from '@fortawesome/free-solid-svg-icons';
import { ErrorLabelComponent } from '../error-label/error-label.component';

@Component({
  selector: 'app-select',
  templateUrl: './select.html',
  imports: [
    NgClass,
    ReactiveFormsModule,
    FormsModule,
    TitleCasePipe,
    FontAwesomeModule,
    ErrorLabelComponent,
  ],
})
export class SelectComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() name = '';
  @Input() className = '';
  @Input() isSubmitted = false;
  @Input() errMsg = '';
  @Input() options: Select[] = [];
  @Input() loading = false;
  @Input() required = false;
  @Output() valueChange = new EventEmitter<string>();

  errors: string[] = [];

  faChevronDown = faChevronDown;

  public controlDir = inject(NgControl, { self: true });

  constructor() {
    this.controlDir.valueAccessor = this;
  }

  sendUpdate(): void {
    this.valueChange.emit(this.control.value);
  }

  onError = () => {
    return HandleErrorsMessages(this.control);
  };

  // Las funciones writeValue, registerOnChange y registerOnTouched son necesarias
  // para el correcto funcionamiento del formControl. Estas retornan nada de momento.

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
