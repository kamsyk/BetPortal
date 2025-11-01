import { Component, inject } from '@angular/core';
import { API_BASE_URL } from '../../../../core/tokens';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { SsrCookieService } from 'ngx-cookie-service-ssr';

@Component({
  selector: 'app-sports-list',
  imports: [],
  templateUrl: './sports-list.component.html',
  styleUrl: './sports-list.component.scss'
})
export class SportsListComponent {
  private http = inject(HttpClient);
  private apiBase = inject(API_BASE_URL);
  private cookieService = inject(SsrCookieService);

  loading = false;
  result: unknown = null;
  error = '';

  testSessionFromCookie() {
    console.log('Testing session from cookie...');
    console.log('API Base URL:', this.apiBase);
    console.log('Full URL:', `${this.apiBase}/auth/session-from-cookie`);
    console.log('Current cookies:', document.cookie);

    this.loading = true;
    this.result = null;
    this.error = '';

    //const authToken = localStorage.getItem('authToken') || '';
    const tokenBrowser = this.cookieService.get('auth_token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${tokenBrowser}`
    });

    this.http.get(`${this.apiBase}/auth/header-token`, {
      //withCredentials: true, // Include cookies
      headers: headers
    }).subscribe({
      next: (response) => {
        console.log('✅ Session from cookie success:', response);
        this.result = response;
        this.loading = false;
      },
      error: (error) => {
        console.error('❌ Session from cookie failed:', error);
        this.error = `${error.status}: ${error.message}`;
        this.loading = false;
      }
    });
  }
}
