import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-access-denied',
  imports: [],
  templateUrl: './access-denied.html',
})
export class AccessDenied {
  private router = inject(Router);

  onGoHome() {
    this.router.navigate(['/dashboard']);
  }
}
