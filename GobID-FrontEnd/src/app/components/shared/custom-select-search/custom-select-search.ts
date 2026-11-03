import { Component, computed, ElementRef, EventEmitter, inject, input, Output, signal, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { Select } from '../../../models';

let selectSearchIdSeq = 0;

export interface SelectOption {
  value: string | number;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-custom-select-search',
  templateUrl: './custom-select-search.html',
})
export class CustomSelectSearch implements ControlValueAccessor {
  constructor() {
    const ngControl = inject(NgControl, { self: true, optional: true });
    if (ngControl) ngControl.valueAccessor = this;

    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(),
    ).subscribe((q) => this.onSearchValue.emit(q));
  }

  @ViewChild('triggerBtn') triggerBtn!: ElementRef<HTMLButtonElement>;

  readonly label = input('');
  readonly options = input<Select[]>([]);
  readonly placeholder = input('Seleccionar...');
  readonly searchPlaceholder = input('Buscar...');
  readonly maxResults = input(10);
  readonly hint = input('');
  readonly errorMessage = input('');
  readonly required = input(false);
  readonly id = input(`custom-select-search-${++selectSearchIdSeq}`);

  @Output() selectedItem = new EventEmitter<string | number>();
  @Output() onSearchValue = new EventEmitter<string | number>();

  protected value = signal<string | number>('');
  protected disabled = signal(false);
  protected isOpen = signal(false);
  protected searchQuery = signal('');
  protected dropdownStyle = signal<{ top: string; left: string; width: string }>({
    top: '0px',
    left: '0px',
    width: '0px',
  });

  private searchSubject = new Subject<string>();

  private onChange: (value: string | number) => void = () => {};
  private onTouched: () => void = () => {};

  protected hasError = computed(() => !!this.errorMessage());

  private allMatching = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    if (!q) return this.options();
    return this.options().filter((o) => o.label.toLowerCase().includes(q));
  });

  protected filteredOptions = computed(() => this.allMatching().slice(0, this.maxResults()));

  protected matchingCount = computed(() => this.allMatching().length);

  protected selectedLabel = computed(
    () => this.options().find((o) => o.value === this.value())?.label ?? '',
  );

  protected triggerClasses = computed(() => {
    const base =
      'flex h-[2.6875rem] text-base w-full cursor-pointer items-center justify-between rounded-[0.3125rem] border bg-white px-3 text-left text-black outline-none transition-colors duration-200 font-roboto normal-case font-[400] ';
    if (this.disabled())
      return `${base} cursor-not-allowed border-bg-dividers bg-text-light-gray`;
    if (this.hasError())
      return `${base} border-alert-error bg-[#ffeeed]`;
    if (this.isOpen())
      return `${base} border-input-hover-border bg-bg-light-blue`;
    return `${base} border-bg-dividers hover:border-input-hover-border hover:bg-bg-light-blue`;
  });

  protected errorId = computed(() => `${this.id()}-error`);

  writeValue(value: string | number): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string | number) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected toggleOpen(): void {
    if (this.disabled()) return;
    if (this.isOpen()) {
      this.close();
    } else {
      const rect = this.triggerBtn.nativeElement.getBoundingClientRect();
      this.dropdownStyle.set({
        top: `${rect.bottom + 4}px`,
        left: `${rect.left}px`,
        width: `${rect.width}px`,
      });
      this.isOpen.set(true);
    }
  }

  protected close(): void {
    this.isOpen.set(false);
    this.searchQuery.set('');
  }

  protected onSearch(event: Event): void {
    const q = (event.target as HTMLInputElement).value;
    this.searchQuery.set(q);
    this.searchSubject.next(q);
  }

  protected selectOption(option: SelectOption): void {
    if (option.disabled) return;
    this.value.set(option.value);
    this.onChange(option.value);
    this.onTouched();
    this.selectedItem.emit(option.value);
    this.close();
  }

  protected clearSelection(event: Event): void {
    event.stopPropagation();
    if (this.disabled()) return;
    this.value.set('');
    this.onChange('');
    this.onTouched();
    this.selectedItem.emit('');
    this.close();
  }
}
