import { Injectable, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);
  private readonly tokenKey = 'auth_token';

  private get isBrowser() { return isPlatformBrowser(this.platformId); }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  getToken(): string | null {
    if (!this.isBrowser) return null;
    try {
      return localStorage.getItem(this.tokenKey);
    } catch {
      return null;
    }
  }

  logout(): void {
    if (!this.isBrowser) return;
    try { localStorage.removeItem(this.tokenKey); } catch {}
  }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password }).pipe(
      map(res => {
        if (this.isBrowser) {
          const token = res.accessToken ?? res.AccessToken ?? '';
          localStorage.setItem(this.tokenKey, token);
        }
        return true;
      }),
      catchError(() => of(false))
    );
  }
}

interface AuthResponse {
  accessToken?: string;
  AccessToken?: string;
  expiresAt?: string | Date;
  tokenType?: string;
};
