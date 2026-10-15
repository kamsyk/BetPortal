import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService, KsSignUpComponent } from 'ks-ag-controls';
import { take } from 'rxjs';
import { API_BASE_URL } from '../../../core/tokens';
import { LanguageService } from '../../services/language.service';

@Component({
  selector: 'app-sign-up',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [KsSignUpComponent, TranslatePipe, RouterLink],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.scss',
})
export class SignUpComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apiBase = inject(API_BASE_URL);
  private readonly authService = inject(AuthService);
  protected readonly currentLanguage = inject(LanguageService).currentLanguage;

  readonly googleAuthUrl = signal('');
  readonly googleReturnUrl = signal('/');
  readonly facebookAuthUrl = signal('');
  readonly facebookReturnUrl = signal('/');
  readonly googleAccountRequired = signal(false);
  readonly facebookAccountRequired = signal(false);

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.googleAccountRequired.set(
      this.route.snapshot.queryParamMap.get('googleError') === 'account-required',
    );
    this.facebookAccountRequired.set(
      this.route.snapshot.queryParamMap.get('facebookError') === 'account-required',
    );
    this.googleReturnUrl.set(returnUrl);
    this.facebookReturnUrl.set(returnUrl);
    this.googleAuthUrl.set(`${this.apiBase}/auth/google/register`);
    this.facebookAuthUrl.set(`${this.apiBase}/auth/facebook/register`);

    this.authService
      .checkAuthStatus()
      .pipe(take(1))
      .subscribe((response) => {
        if (response.isAuthenticated) {
          this.router.navigateByUrl(returnUrl, { replaceUrl: true });
        }
      });
  }
}
