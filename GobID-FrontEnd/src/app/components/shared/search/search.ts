import { NgClass } from '@angular/common';
import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faSearch, faX } from '@fortawesome/free-solid-svg-icons';
import { NgxMaskDirective, provideNgxMask } from 'ngx-mask';

@Component({
  selector: 'app-search',
  imports: [FontAwesomeModule, FormsModule, NgClass, NgxMaskDirective],
  templateUrl: './search.html',
  providers: [provideNgxMask()],
})
export class SearchComponent {
  @Input() placeholder = 'Buscar...';
  @Input() mask = '';
  @Input() errMsg = '';
  @Input() useButton = false;
  @Output() searchValue = new EventEmitter<string>();
  protected value = signal('');
  protected flag = signal(false);
  protected storedValue = signal('');
  faSearch = faSearch;
  faX = faX;

  clearSearch() {
    this.flag.set(false);
    this.value.set('');
    this.searchValue.emit(this.value());
  }

  onChangeValue = ($event: Event) => {
    const input = $event.target as HTMLInputElement;
    if (input.value === '') {
      this.flag.set(false);
      if (!this.useButton) {
        this.searchValue.emit(this.value());
      }
    }
    if (this.value() !== this.storedValue()) {
      this.flag.set(false);
    }
  };

  onEnterSearch = () => {
    if (this.useButton) return;
    this.onSearchValue();
  };

  onSearchValue = () => {
    if (!this.value()) return;
    this.flag.set(true);
    this.storedValue.set(this.value());
    this.searchValue.emit(this.value());
  };
}
