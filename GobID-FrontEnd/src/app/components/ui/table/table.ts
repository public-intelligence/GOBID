import { Component, Input, OnChanges, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import {
  faEllipsisVertical,
  faFaceSadTear,
  faNotdef,
} from '@fortawesome/free-solid-svg-icons';
import { ClickOutsideDirective } from '../../../directives/click-outside.directive';
import { Column, ColumnData, DisplayData } from '../../../models';

@Component({
  selector: 'app-table',
  imports: [NgClass, FontAwesomeModule, ClickOutsideDirective],
  templateUrl: './table.html',
})
export class TableComponent implements OnChanges {
  faEllipsis = faEllipsisVertical;
  faFaceSadTear = faFaceSadTear;
  faNotdef = faNotdef;

  @Input() headers: Column[] = [];
  @Input() columnData: ColumnData[] = [];
  @Input() actions = true;
  @Input() showIcons = false;
  @Input() isLoading = false;
  @Input() noDataDescription = 'No hay datos para mostrar...';
  @Input() editAction!: (id: string) => void;
  @Input() deleteAction!: (id: string) => void;
  @Input() onViewInfo!: (data: DisplayData[], editPath: string) => void;

  protected data = signal<ColumnData[]>([]);
  protected dropdownPosition = signal<{ top: number; left: number }>({
    top: 0,
    left: 0,
  });

  ngOnChanges(): void {
    this.data.set(this.columnData);
  }

  toggleActions = ({
    id,
    bool,
    event,
  }: {
    id: string;
    bool: boolean;
    event?: MouseEvent;
  }): void => {
    if (bool && event) {
      const rect = (event.currentTarget as HTMLElement).getBoundingClientRect();
      this.dropdownPosition.set({ top: rect.bottom, left: rect.right });
    }
    const elements = this.columnData.map((item, index) => {
      if (`${item.id}-${index}` === id) {
        return { ...item, showActions: bool };
      }
      return { ...item, showActions: false };
    });
    this.data.set(elements);
  };
}
