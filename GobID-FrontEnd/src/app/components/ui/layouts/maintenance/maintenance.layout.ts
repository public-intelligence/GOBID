import {
  Component,
  EventEmitter,
  inject,
  Input,
  Output,
  ViewChild,
} from '@angular/core';
import { SearchComponent } from '../../../shared/search/search';
import { TableComponent } from '../../table/table';
import {
  ApplicationFilter,
  Column,
  ColumnData,
  DisplayData,
  Filter,
  OrganizationFilter,
  UserFilter,
} from '../../../../models';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { ViewInfo } from '../../modals/view-info/view-info';
import { FilterComponent } from '../../filter/filter';
import { Paginator } from '../../paginator/paginator';
import { faPlus } from '@fortawesome/free-solid-svg-icons';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';

@Component({
  selector: 'app-maintenance',
  imports: [
    SearchComponent,
    TableComponent,
    NgClass,
    ViewInfo,
    FilterComponent,
    Paginator,
    FaIconComponent,
  ],
  templateUrl: './maintenance.layout.html',
})
export class Maintenance {
  @ViewChild(ViewInfo) viewInfoModal!: ViewInfo;

  faPlus = faPlus;

  // Title prop
  @Input() title = 'Titulo por Defecto';
  @Input() subTitle = '';
  @Input() titleInfo = 'Información detallada';

  // button props
  @Input() buttonText = 'Agregar Nuevo';
  @Input() createPath!: string;
  @Input() showButton = true;
  @Input() onCreate?: () => void;

  // Search props
  @Output() searchValue = new EventEmitter<string>();

  // filter props
  @Input() filters: Filter[] = [];
  @Output() filterSelected = new EventEmitter<
    OrganizationFilter | ApplicationFilter | UserFilter
  >();
  @Input() showFilters = false;

  // Table props
  @Input() headers: Column[] = [];
  @Input() columnData: ColumnData[] = [];
  @Input() actions = true;
  @Input() toggleActions!: ({
    id,
    bool,
  }: {
    id: string;
    bool: boolean;
  }) => void;
  @Input() editAction!: (id: string) => void;
  @Input() deleteAction!: (id: string) => void;
  @Input() showIcons = false;

  // pagination
  @Input() showPaginator = false;
  @Input() pageNumber = 0;
  @Input() totalCount = 0;
  @Input() totalPages = 0;
  @Input() pageSizeInput = 10;
  @Output() actualPage = new EventEmitter<number>();
  @Output() pageSize = new EventEmitter<number>();

  @Input() isLoading = false;

  @Input() canCreate = true;
  @Input() canEdit = true;

  private router = inject(Router);

  onNavigate() {
    this.router.navigate([this.createPath]);
  }

  onViewInfo = (data: DisplayData[], editPath = '') => {
    this.viewInfoModal.openModal(data, editPath);
  };

  searchedValue = async (value: string) => {
    this.searchValue.emit(value);
  };

  SelectedFilter(value: OrganizationFilter | ApplicationFilter | UserFilter) {
    this.filterSelected.emit(value);
  }

  actalPageNumber(page: number) {
    this.actualPage.emit(page);
  }

  pageSizeChange(size: number) {
    this.pageSize.emit(size);
  }
}
