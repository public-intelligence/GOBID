import {
  Component,
  EventEmitter,
  Input,
  Output,
  signal,
  OnInit,
} from '@angular/core';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NgxPaginationModule } from 'ngx-pagination';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faAngleLeft, faAngleRight } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-paginator',
  imports: [NgClass, FormsModule, NgxPaginationModule, FontAwesomeModule],
  templateUrl: './paginator.html',
})
export class Paginator implements OnInit {
  @Input() pageNumber = 1;
  @Input() totalCount = 0;
  @Input() totalPages = 0;
  @Input() pageSizeInput = 10;
  @Output() pageToEmit = new EventEmitter<number>();
  @Output() pageSize = new EventEmitter<number>();

  protected currentSize = signal<number>(10);
  protected pagaSizeRange: number[] = [];
  protected dummyArray = [1];
  protected faArrowLeft = faAngleLeft;
  protected faArrowRight = faAngleRight;

  ngOnInit() {
    this.currentSize.set(this.pageSizeInput || 10);
    this.sizeRange(10);
  }

  sizeRange(size: number) {
    this.pagaSizeRange = Array.from({ length: size }, (_, i) => (i + 1) * 5);
  }

  emitPageSize(size: number) {
    // Need to parse string to number for select dropdowns
    const parsedSize = Number(size);
    this.currentSize.set(parsedSize);
    this.pageSize.emit(parsedSize);
    this.pageToEmit.emit(1);
  }

  onPageChange(page: number) {
    this.pageToEmit.emit(page);
  }
}
