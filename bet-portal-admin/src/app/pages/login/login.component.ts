import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { API_BASE_URL } from '../../../core/tokens';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../services/auth.service';
import { KsLoginComponent } from "ks-ag-controls";

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  imports: [KsLoginComponent],
})

export class LoginComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private apiBase = inject(API_BASE_URL);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);
  private readonly email = "test@local";
  private readonly password = "Pass@123";

  loading = false;
  error = '';
  readonly loginUrl = signal<string>('');
  readonly redirectUrl = signal<string>('');

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.redirectUrl.set(returnUrl);
    this.loginUrl.set(`${this.apiBase}/auth/login`);
  }

  onLoginSuccess(event: { email: string }) {
    debugger;
    console.log('Login successful!', event);
    this.authService
      .login(this.email, this.password, this.loginUrl(), this.redirectUrl())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ok) => {
          this.loading = false;
          if (ok) {
            console.log('User logged in successfully');
          } else {
            this.error = 'Invalid credentials';
          }
        },
        error: () => {
          this.loading = false;
          this.error = 'An error occurred during login';
        },
      });
  }
}
