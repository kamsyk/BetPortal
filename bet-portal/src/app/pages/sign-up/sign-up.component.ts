import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService, KsSignUpComponent, type KsAuthProvider } from 'ks-ag-controls';
import { take } from 'rxjs';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthDialogRequest, AuthDialogService } from '../../services/auth-dialog.service';
import { LanguageService } from '../../services/language.service';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-sign-up',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [KsSignUpComponent, TranslatePipe],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.scss',
})
export class SignUpComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly apiBase = inject(API_BASE_URL);
  private readonly authService = inject(AuthService);
  private readonly authDialog = inject(AuthDialogService);
  private readonly dialogRef = inject(MatDialogRef<SignUpComponent>, { optional: true });
  private readonly data = inject<AuthDialogRequest>(MAT_DIALOG_DATA, { optional: true });
  private readonly themeService = inject(ThemeService);
  protected readonly currentLanguage = inject(LanguageService).currentLanguage;
  protected readonly themeMode = computed(() =>
    this.themeService.isDarkMode() ? 'dark' : 'light',
  );

  readonly providers: readonly KsAuthProvider[] = ['google', 'facebook'];
  readonly googleAuthUrl = signal('');
  readonly googleReturnUrl = signal('/');
  readonly facebookAuthUrl = signal('');
  readonly facebookReturnUrl = signal('/');
  readonly googleAccountRequired = signal(false);
  readonly facebookAccountRequired = signal(false);

  ngOnInit(): void {
    const returnUrl = this.value('returnUrl') || '/';
    this.googleAccountRequired.set(this.value('googleError') === 'account-required');
    this.facebookAccountRequired.set(this.value('facebookError') === 'account-required');
    this.googleReturnUrl.set(returnUrl);
    this.facebookReturnUrl.set(returnUrl);
    this.googleAuthUrl.set(`${this.apiBase}/auth/google/register`);
    this.facebookAuthUrl.set(`${this.apiBase}/auth/facebook/register`);

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

  closeDialog(): void {
    this.dialogRef?.close();
  }

  openLogin(): void {
    this.authDialog.openLogin({ returnUrl: this.googleReturnUrl() });
  }

  private value(key: 'returnUrl' | 'googleError' | 'facebookError'): string | null {
    if (this.data) {
      return this.data[key] ?? null;
    }

    return this.route.snapshot.queryParamMap.get(key);
  }
}
