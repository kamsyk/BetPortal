import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { KsLoginComponent } from 'ks-ag-controls';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { API_BASE_URL } from '../../../core/tokens';
//import { RESPONSE } from '@angular/ssr';
//mport type { Response } from 'express';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatInputModule,
    MatButtonModule,
    KsLoginComponent,
  ],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  private fb = inject(FormBuilder);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private apiBase = inject(API_BASE_URL);
  //private response = inject(Response, { optional: true }); // For SSR cookie setting

  loading = false;
  error = '';
  readonly loginUrl = signal<string>('');
  readonly redirectUrl = signal<string>('');

  // form = this.fb.group({
  //   email: ['', [Validators.required, Validators.email]],
  //   password: ['', [Validators.required]],
  // });

  ngOnInit(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
    this.redirectUrl.set(returnUrl);
    this.loginUrl.set(`${this.apiBase}/auth/login`);
  }

  onLoginSuccess(event: { email: string }) {
    console.log('Login successful!', event);
    // Handle success (e.g., show toast, navigate, etc.)
  }

  //  private setServerCookie(name: string, value: string, days = 7): void {
  //   if (!this.response) return;

  //   const expires = new Date();
  //   expires.setTime(expires.getTime() + days * 24 * 60 * 60 * 1000);

  //   const cookieOptions = [
  //     `${name}=${encodeURIComponent(value)}`,
  //     `expires=${expires.toUTCString()}`,
  //     'path=/',
  //     'SameSite=Strict',
  //     'Secure',
  //     'HttpOnly' // Server-side cookies can be HttpOnly for security
  //   ];

  //   this.response.setHeader('Set-Cookie', cookieOptions.join('; '));
  // }
}
