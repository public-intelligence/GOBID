import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-details-panel',
  imports: [],
  templateUrl: './details-panel.html',
})
export class DetailsPanel {
  @Input() title = '[title]';
  @Input() description = '[description]';
}
