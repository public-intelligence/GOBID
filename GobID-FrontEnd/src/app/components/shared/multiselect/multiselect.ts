import {
  Component,
  ElementRef,
  HostListener,
  inject,
  Input,
  signal,
} from '@angular/core';
import {
  ControlValueAccessor,
  FormControl,
  NgControl,
} from '@angular/forms';
import { NgClass } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faCheck, faXmark } from '@fortawesome/free-solid-svg-icons';

export interface MultiSelectOption {
  id: number;
  label: string;
}

@Component({
  selector: 'app-multiselect',
  templateUrl: './multiselect.html',
  imports: [NgClass, FontAwesomeModule],
})
export class MultiSelectComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = 'Seleccionar opciones';
  @Input() loading = false;
  @Input() options: MultiSelectOption[] = [];

  protected isOpen = signal(false);
  protected selectedIds = signal<number[]>([]);
  protected selectedOptions = signal<MultiSelectOption[]>([]);
  protected faCheck = faCheck;
  protected faXmark = faXmark;
  protected dropdownPos = signal({ top: '0px', left: '0px', width: '0px' });

  private elementRef = inject(ElementRef);
  public controlDir = inject(NgControl, { self: true });

  constructor() {
    this.controlDir.valueAccessor = this;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }

  toggleDropdown() {
    if (!this.isOpen()) {
      const trigger = this.elementRef.nativeElement.querySelector(
        '[data-trigger]',
      ) as HTMLElement;
      const rect = trigger.getBoundingClientRect();
      this.dropdownPos.set({
        top: `${rect.bottom + 4}px`,
        left: `${rect.left}px`,
        width: `${rect.width}px`,
      });
    }
    this.isOpen.update((v) => !v);
  }

  toggleOption(id: number) {
    const current = this.selectedIds();
    const updated = current.includes(id)
      ? current.filter((i) => i !== id)
      : [...current, id];
    this.selectedIds.set(updated);
    this.selectedOptions.set(this.options.filter((o) => updated.includes(o.id)));
    this.control.setValue(updated);
    this.control.markAsTouched();
  }

  removeOption(id: number, event: Event) {
    event.stopPropagation();
    const updated = this.selectedIds().filter((i) => i !== id);
    this.selectedIds.set(updated);
    this.selectedOptions.set(this.options.filter((o) => updated.includes(o.id)));
    this.control.setValue(updated);
  }

  isSelected(id: number): boolean {
    return this.selectedIds().includes(id);
  }

  writeValue(value: number[] | null): void {
    const ids = value ?? [];
    this.selectedIds.set(ids);
    this.selectedOptions.set(this.options.filter((o) => ids.includes(o.id)));
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
