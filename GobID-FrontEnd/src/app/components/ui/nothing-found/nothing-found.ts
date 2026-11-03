import { Component } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faFaceSadTear } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-nothing-found',
  imports: [FontAwesomeModule],
  templateUrl: './nothing-found.html',
})
export class NothingFound {
  faFaceSadTear = faFaceSadTear;
}
