import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthService, KsLoginComponent } from 'ks-ag-controls';
import { Router } from '@angular/router';
import { take } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [KsLoginComponent],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})

export class LoginComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private apiBase = inject(API_BASE_URL);
  private authService = inject(AuthService);

  loading = false;
  error = '';
  readonly loginUrl = signal<string>('');
  readonly redirectUrl = signal<string>('');

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.redirectUrl.set(returnUrl);
    this.loginUrl.set(`${this.apiBase}/auth/login`);

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
