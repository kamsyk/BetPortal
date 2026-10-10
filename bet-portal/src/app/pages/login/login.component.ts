import { afterNextRender, ChangeDetectionStrategy, Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthService, KsLoginComponent } from 'ks-ag-controls';
import { Router } from '@angular/router';
import { take } from 'rxjs';
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
  private readonly loginControl = viewChild.required(KsLoginComponent);

  constructor() {
    afterNextRender(() => {
      if (this.googleAccountRequired()) {
        this.loginControl().openSignUp();
      }
    });
  }

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.googleAccountRequired.set(this.route.snapshot.queryParamMap.get('googleError') === 'account-required');
    this.redirectUrl.set(returnUrl);
    this.loginUrl.set(`${this.apiBase}/auth/login`);
    this.googleAuthUrl.set(`${this.apiBase}/auth/google`);
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
