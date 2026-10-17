import { isPlatformBrowser } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  PLATFORM_ID,
  signal,
  TemplateRef,
  viewChild,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthService, KsLoginComponent, KsSignUpComponent } from 'ks-ag-controls';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { asapScheduler, observeOn, take } from 'rxjs';
import { LanguageService } from '../../services/language.service';
import { ThemeService } from '../../services/theme.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [KsLoginComponent, TranslatePipe],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})

export class LoginComponent implements OnInit, AfterViewInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private apiBase = inject(API_BASE_URL);
  private authService = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly themeService = inject(ThemeService);
  private readonly loginDialog = viewChild.required<TemplateRef<void>>('loginDialog');
  protected readonly currentLanguage = inject(LanguageService).currentLanguage;
  protected readonly themeMode = computed(() =>
    this.themeService.isDarkMode() ? 'dark' : 'light',
  );

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

  private dialogRef?: MatDialogRef<unknown>;
  private viewReady = false;
  private openWhenReady = false;
  private leaving = false;

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.leaving = true;
      this.dialogRef?.close();
    });

    // lib-ks-login still opens Sign up in a dialog; route to the page instead.
    this.dialog.afterOpened.pipe(observeOn(asapScheduler), takeUntilDestroyed()).subscribe(dialog => {
      if (dialog.componentInstance instanceof KsSignUpComponent) {
        const returnUrl = this.redirectUrl() || '/';
        dialog.close();
        this.leave(() => {
          void this.router.navigate(['/sign-up'], {
            queryParams: returnUrl === '/' ? undefined : { returnUrl },
          });
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
      this.leave(() => {
        void this.router.navigate(['/sign-up'], {
          queryParams: {
            ...(returnUrl !== '/' ? { returnUrl } : {}),
            ...(googleError ? { googleError } : {}),
            ...(facebookError ? { facebookError } : {}),
          },
          replaceUrl: true,
        });
      });
      return;
    }

    this.authService
      .checkAuthStatus()
      .pipe(take(1))
      .subscribe((response) => {
        if (response.isAuthenticated) {
          this.leave(() => {
            void this.router.navigateByUrl(returnUrl, { replaceUrl: true });
          });
          return;
        }

        this.openWhenReady = true;
        this.openDialog();
      });
  }

  ngAfterViewInit(): void {
    if (!this.isBrowser) {
      return;
    }

    this.viewReady = true;
    this.openDialog();
  }

  closeDialog(): void {
    this.dialogRef?.close();
  }

  onLoginSuccess(event: { username: string }) {
    console.log('Login successful!', event);
  }

  private openDialog(): void {
    if (!this.isBrowser || this.leaving || !this.viewReady || !this.openWhenReady || this.dialogRef) {
      return;
    }

    const dialogRef = this.dialog.open(this.loginDialog(), {
      width: '468px',
      maxWidth: 'calc(100vw - 32px)',
      maxHeight: 'calc(100vh - 32px)',
      autoFocus: false,
      restoreFocus: true,
      ariaModal: true,
      panelClass: 'login-dialog-panel',
    });
    this.dialogRef = dialogRef;
    dialogRef.afterOpened().subscribe(() => {
      this.labelDialog(dialogRef.id);
      this.focusUsername(dialogRef.id);
    });
    dialogRef.afterClosed().subscribe(() => {
      this.dialogRef = undefined;
      if (!this.leaving) {
        void this.router.navigateByUrl('/');
      }
    });
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

  private leave(navigate: () => void): void {
    this.leaving = true;
    this.dialogRef?.close();
    navigate();
  }
}
