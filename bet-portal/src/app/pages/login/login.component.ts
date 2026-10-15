import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthService, KsLoginComponent, KsSignUpComponent } from 'ks-ag-controls';
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { asapScheduler, observeOn, take } from 'rxjs';
import { LanguageService } from '../../services/language.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [KsLoginComponent, TranslatePipe],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})

export class LoginComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private apiBase = inject(API_BASE_URL);
  private authService = inject(AuthService);
  protected readonly currentLanguage = inject(LanguageService).currentLanguage;

  loading = false;
  error = '';
  readonly loginUrl = signal<string>('');
  readonly redirectUrl = signal<string>('');
  readonly googleAuthUrl = signal<string>('');
  readonly googleReturnUrl = signal<string>('');
  readonly googleAccountRequired = signal(false);
  readonly googleAccountExists = signal(false);
  readonly facebookAuthUrl = signal<string>('');
  readonly facebookAccountRequired = signal(false);
  readonly facebookAccountExists = signal(false);

  constructor() {
    // lib-ks-login still opens Sign up in a dialog; route to the page instead.
    inject(MatDialog).afterOpened.pipe(observeOn(asapScheduler), takeUntilDestroyed()).subscribe(dialog => {
      if (dialog.componentInstance instanceof KsSignUpComponent) {
        const returnUrl = this.redirectUrl() || '/';
        dialog.close();
        void this.router.navigate(['/sign-up'], {
          queryParams: returnUrl === '/' ? undefined : { returnUrl },
        });
      }
    });
  }

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    const googleError = this.route.snapshot.queryParamMap.get('googleError');
    const facebookError = this.route.snapshot.queryParamMap.get('facebookError');
    const openSignUp = this.route.snapshot.queryParamMap.get('signUp') === 'true';

    this.googleAccountRequired.set(googleError === 'account-required');
    this.googleAccountExists.set(googleError === 'account-exists');
    this.facebookAccountRequired.set(facebookError === 'account-required');
    this.facebookAccountExists.set(facebookError === 'account-exists');
    this.redirectUrl.set(returnUrl);
    this.loginUrl.set(`${this.apiBase}/auth/login`);
    this.googleAuthUrl.set(`${this.apiBase}/auth/google`);
    this.facebookAuthUrl.set(`${this.apiBase}/auth/facebook`);
    this.googleReturnUrl.set(returnUrl);

    if (openSignUp || this.googleAccountRequired() || this.facebookAccountRequired()) {
      void this.router.navigate(['/sign-up'], {
        queryParams: {
          ...(returnUrl !== '/' ? { returnUrl } : {}),
          ...(googleError ? { googleError } : {}),
          ...(facebookError ? { facebookError } : {}),
        },
        replaceUrl: true,
      });
      return;
    }

    this.authService
      .checkAuthStatus()
      .pipe(take(1))
      .subscribe((response) => {
        if (response.isAuthenticated) {
          this.router.navigateByUrl(returnUrl, { replaceUrl: true });
        }
      });
  }

  onLoginSuccess(event: { username: string }) {
    console.log('Login successful!', event);
  }
}
