import {
  Component,
  EventEmitter,
  inject,
  Input,
  OnInit,
  Output,
  signal,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFilter, faX, faAngleDown } from '@fortawesome/free-solid-svg-icons';
import {
  ApplicationFilter,
  Filter,
  OrganizationFilter,
  UserFilter,
} from '../../../models';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {} from '../../shared';
import { ClickOutsideDirective } from '../../../directives/click-outside.directive';

@Component({
  selector: 'app-filter',
  imports: [
    FontAwesomeModule,
    FormsModule,
    ReactiveFormsModule,
    ClickOutsideDirective,
    NgClass,
  ],
  templateUrl: './filter.html',
})
export class FilterComponent implements OnInit {
  @Output() filterSelectedChange = new EventEmitter<
    OrganizationFilter | ApplicationFilter | UserFilter
  >();
  @Input() filters: Filter[] = [];

  private fb = inject(FormBuilder);
  private filterValue = signal<Record<string, string | number>>({});

  protected faFilter = faFilter;
  protected showFilters = signal<boolean>(false);
  protected filterForm = this.fb.group({});
  protected faX = faX;
  protected isFiltered = signal<boolean>(false);

  // ahora cada filtro tiene su propio valor seleccionado
  protected filterSelected = signal<Record<string, string>>({});
  protected filterName = signal('');
  protected faAngleDown = faAngleDown;
  protected openFilter = signal<string | null>(null);

  ngOnInit() {
    this.setInitialValue();
  }

  setInitialValue() {
    const selected: Record<string, string> = {};
    const values: Record<string, string | number> = {};

    this.filters.forEach((filter) => {
      const item = filter.values?.find((v) => v.value === filter.defaultValue);
      selected[filter.controlName] = item ? item.label : 'SIN FILTRO';
      values[filter.controlName] = item ? item.value : '';
    });

    this.filterSelected.set(selected);
    this.filterValue.set(values);
  }

  getValue(controlName: string, label: string) {
    const filter = this.filters.find((f) => f.controlName === controlName);
    if (!filter) return;

    const item = filter.values?.find((i) => i.label === label);
    if (item) {
      this.filterValue.update((fv) => ({ ...fv, [controlName]: item.value }));
      this.filterSelected.update((fs) => ({ ...fs, [controlName]: label }));
    }
  }

  applyFilters(controlName: string, label: string) {
    const filter = this.filters.find((f) => f.controlName === controlName);
    if (!filter) return;

    const item = filter.values?.find((i) => i.label === label);
    if (item) {
      this.filterValue.update((fv) => ({ ...fv, [controlName]: item.value }));
      this.filterSelected.update((fs) => ({ ...fs, [controlName]: label }));
    }

    // Emit the FULL state of all filters so the parent can filter by multiple at once
    this.filterSelectedChange.emit(this.filterValue());
    this.isFiltered.set(true);
    this.openFilter.set(null); // cerrar el dropdown al seleccionar
  }

  toggleFilter(controlName: string) {
    this.openFilter.update((current) =>
      current === controlName ? null : controlName,
    );
  }

  closeFilter() {
    this.openFilter.set(null);
  }

  resetFilter() {
    const resetSelected: Record<string, string> = {};
    const resetValues: Record<string, string | number> = {};

    this.filters.forEach((filter) => {
      this.filterForm.get(filter.controlName)?.setValue(filter.defaultValue);

      const item = filter.values?.find((v) => v.value === filter.defaultValue);
      if (item) {
        resetSelected[filter.controlName] = item.label;
        resetValues[filter.controlName] = item.value;
      } else {
        resetSelected[filter.controlName] = '';
      }
    });

    this.filterSelected.set(resetSelected);
    this.filterValue.set(resetValues);

    this.isFiltered.set(false);
    this.filterSelectedChange.emit(this.filterValue());
  }
}
