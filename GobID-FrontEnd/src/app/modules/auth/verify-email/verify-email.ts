import { Component, inject, OnInit, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../services';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-verify-email',
  imports: [TranslocoModule],
  templateUrl: './verify-email.html',
})
export class VerifyEmail implements OnInit {
  protected isLoading = signal(true);
  protected isSuccess = signal(false);
  protected errorMessage = signal('');

  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private transloco = inject(TranslocoService);

  ngOnInit() {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.isLoading.set(false);
      this.errorMessage.set(this.transloco.translate('verify.invalidLink'));
      return;
    }
    this.confirmEmail(token);
  }

  private async confirmEmail(token: string) {
    try {
      await firstValueFrom(this.authService.onConfirmEmail(token));
      this.isSuccess.set(true);
    } catch (error: any) {
      this.errorMessage.set(this.transloco.translate('verify.invalidLink'));
    } finally {
      this.isLoading.set(false);
    }
  }

  navigateToLogin() {
    this.router.navigate(['/']);
  }
}
