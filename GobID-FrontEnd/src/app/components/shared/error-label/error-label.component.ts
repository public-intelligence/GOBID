import { Component, Input } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faChevronDown } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-error-label',
  imports: [FontAwesomeModule],
  templateUrl: './error-label.component.html',
})
export class ErrorLabelComponent {
  @Input() show = false;
  @Input() errors: string[] = [];

  faChevronDown = faChevronDown;
}
