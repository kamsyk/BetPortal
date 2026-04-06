import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { API_BASE_URL } from '../../../core/tokens';
import { KsLoginComponent } from 'ks-ag-controls';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [KsLoginComponent],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})

export class LoginComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private apiBase = inject(API_BASE_URL);

  loading = false;
  error = '';
  readonly loginUrl = signal<string>('');
  readonly redirectUrl = signal<string>('');

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.redirectUrl.set(returnUrl);
    this.loginUrl.set(`${this.apiBase}/auth/login`);
  }

  onLoginSuccess(event: { username: string }) {
    console.log('Login successful!', event);
  }
}
