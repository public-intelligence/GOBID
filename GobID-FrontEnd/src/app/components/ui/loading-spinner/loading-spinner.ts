import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  imports: [],
  templateUrl: './loading-spinner.html',
})
export class LoadingSpinner {
  @Input() message = 'Cargando…';
}
