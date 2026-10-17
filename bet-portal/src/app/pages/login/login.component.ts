import { isPlatformBrowser } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  PLATFORM_ID,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { asapScheduler, observeOn, take } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService, KsLoginComponent, KsSignUpComponent } from 'ks-ag-controls';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthDialogRequest, AuthDialogService } from '../../services/auth-dialog.service';
import { LanguageService } from '../../services/language.service';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [KsLoginComponent, TranslatePipe],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit, AfterViewInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apiBase = inject(API_BASE_URL);
  private readonly authService = inject(AuthService);
  private readonly authDialog = inject(AuthDialogService);
  private readonly dialogRef = inject(MatDialogRef<LoginComponent>, { optional: true });
  private readonly data = inject<AuthDialogRequest>(MAT_DIALOG_DATA, { optional: true });
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly themeService = inject(ThemeService);
  protected readonly currentLanguage = inject(LanguageService).currentLanguage;
  protected readonly themeMode = computed(() =>
    this.themeService.isDarkMode() ? 'dark' : 'light',
  );

  readonly loginUrl = signal('');
  readonly redirectUrl = signal('');
  readonly googleAuthUrl = signal('');
  readonly googleReturnUrl = signal('');
  readonly googleAccountRequired = signal(false);
  readonly googleAccountExists = signal(false);
  readonly facebookAuthUrl = signal('');
  readonly facebookAccountRequired = signal(false);
  readonly facebookAccountExists = signal(false);

  constructor() {
    // lib-ks-login still opens Sign up in its own dialog; switch to the portal dialog instead.
    inject(MatDialog).afterOpened.pipe(observeOn(asapScheduler), takeUntilDestroyed()).subscribe(dialog => {
      if (dialog.componentInstance instanceof KsSignUpComponent) {
        dialog.close();
        this.authDialog.openSignUp({ returnUrl: this.redirectUrl() || '/' });
      }
    });
  }

  ngOnInit(): void {
    const returnUrl = this.value('returnUrl') || '/';
    const googleError = this.value('googleError');
    const facebookError = this.value('facebookError');
    const openSignUp = this.data?.signUp === true || this.value('signUp') === 'true';

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
      this.authDialog.openSignUp({
        returnUrl,
        googleError: this.googleAccountRequired() ? googleError : null,
        facebookError: this.facebookAccountRequired() ? facebookError : null,
      });
      return;
    }

    this.authService
      .checkAuthStatus()
      .pipe(take(1))
      .subscribe((response) => {
        if (response.isAuthenticated) {
          this.dialogRef?.close();
          void this.router.navigateByUrl(returnUrl, { replaceUrl: true });
        }
      });
  }

  ngAfterViewInit(): void {
    if (!this.isBrowser || !this.dialogRef) {
      return;
    }

    this.labelDialog(this.dialogRef.id);
    this.focusUsername(this.dialogRef.id);
  }

  closeDialog(): void {
    this.dialogRef?.close();
  }

  onLoginSuccess(event: { username: string }): void {
    console.log('Login successful!', event);
  }

  private value(key: 'returnUrl' | 'googleError' | 'facebookError' | 'signUp'): string | null {
    if (this.data && key !== 'signUp') {
      const provided = this.data[key];
      return typeof provided === 'string' ? provided : null;
    }

    return this.route.snapshot.queryParamMap.get(key);
  }

  private labelDialog(dialogId: string): void {
    const container = document.getElementById(dialogId);
    const heading = container?.querySelector('h2');
    if (!container || !heading) {
      return;
    }

    heading.id = 'login-dialog-title';
    container.setAttribute('aria-labelledby', heading.id);
  }

  private focusUsername(dialogId: string): void {
    const container = document.getElementById(dialogId);
    const username = container?.querySelector<HTMLElement>('input[autocomplete="username"]');
    (username ?? container)?.focus();
  }
}
