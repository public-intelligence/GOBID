import { Component, inject, Input, signal, OnInit } from '@angular/core';
import { FontAwesomeModule } from '@fortawesome/angular-fontawesome';
import { faBars } from '@fortawesome/free-solid-svg-icons';
import { TokenDecoder } from '../../../services/token-decoder/token-decoder';

@Component({
  selector: 'app-navbar',
  imports: [FontAwesomeModule],
  templateUrl: './navbar.html',
})
export class Navbar implements OnInit {
  @Input() toggleMobileSidebar!: () => void;

  faBars = faBars;
  protected organizationName = signal('');
  protected aplicationName = 'Sistema de gobId';

  private tokenDecoder = inject(TokenDecoder);

  ngOnInit() {
    this.organizationName.set(this.tokenDecoder.getOrganizationName());
  }
}
