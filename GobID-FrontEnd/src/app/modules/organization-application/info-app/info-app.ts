import { Component, Input } from '@angular/core';
import { ApplicationAdapted } from '../../../models';

@Component({
  selector: 'app-info-app',
  imports: [],
  templateUrl: './info-app.html',
})
export class InfoApp {
  @Input() application: ApplicationAdapted | null = null;
}
