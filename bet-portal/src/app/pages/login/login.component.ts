import { afterNextRender, ChangeDetectionStrategy, Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthService, KsLoginComponent, KsSignUpComponent } from 'ks-ag-controls';
import { MatDialog } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
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
  readonly openSignUpOnLoad = signal(false);
  private readonly loginControl = viewChild.required(KsLoginComponent);

  constructor() {
    inject(MatDialog).afterOpened.pipe(observeOn(asapScheduler), takeUntilDestroyed()).subscribe(dialog => {
      if (dialog.componentInstance instanceof KsSignUpComponent) {
        dialog.componentRef?.setInput('googleAuthUrl', `${this.apiBase}/auth/google/register`);
        dialog.componentRef?.setInput('facebookAuthUrl', `${this.apiBase}/auth/facebook/register`);
      }
    });
    afterNextRender(() => {
      if (
        this.googleAccountRequired()
        || this.facebookAccountRequired()
        || this.openSignUpOnLoad()
      ) {
        this.loginControl().openSignUp();
      }
    });
  }

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.googleAccountRequired.set(this.route.snapshot.queryParamMap.get('googleError') === 'account-required');
    this.googleAccountExists.set(this.route.snapshot.queryParamMap.get('googleError') === 'account-exists');
    this.facebookAccountRequired.set(this.route.snapshot.queryParamMap.get('facebookError') === 'account-required');
    this.facebookAccountExists.set(this.route.snapshot.queryParamMap.get('facebookError') === 'account-exists');
    this.openSignUpOnLoad.set(this.route.snapshot.queryParamMap.get('signUp') === 'true');
    this.redirectUrl.set(returnUrl);
    this.loginUrl.set(`${this.apiBase}/auth/login`);
    this.googleAuthUrl.set(`${this.apiBase}/auth/google`);
    this.facebookAuthUrl.set(`${this.apiBase}/auth/facebook`);
    this.googleReturnUrl.set(returnUrl);

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
