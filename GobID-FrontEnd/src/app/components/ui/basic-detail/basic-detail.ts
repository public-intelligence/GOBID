import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-basic-detail',
  imports: [],
  templateUrl: './basic-detail.html',
})
export class BasicDetail {
  @Input() title = '';
  @Input() description = '';
}
