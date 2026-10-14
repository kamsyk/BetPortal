import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, map, catchError, of, tap, take } from 'rxjs';
import { API_BASE_URL } from '../../core/tokens';


interface User {
  id: number;
  email: string;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private apiBase = inject(API_BASE_URL);
  private platformId = inject(PLATFORM_ID);

  private userSubject = new BehaviorSubject<User | null>(null);
  public user$ = this.userSubject.asObservable();

  constructor() {
    // Check session on app start (browser only)
    if (isPlatformBrowser(this.platformId)) {
      this.validateSession().subscribe();
    }
  }

  validateSession(): Observable<boolean> {
    console.log('[AuthService] Validating session...');
    console.log('[AuthService] Document cookies:', document.cookie); // Check what cookies exist
    return this.http.get<{success: boolean; user: User}>(`${this.apiBase}/auth/me`).pipe(
      map(response => {
        console.log('[AuthService] Session response:', response);
        if (response.success && response.user) {
          this.userSubject.next(response.user);
          return true;
        } else {
          this.userSubject.next(null);
          return false;
        }
      }),
      catchError(error => {
        console.log('[AuthService] Session validation failed:', error.status);
        this.userSubject.next(null);
        return of(false);
      })
    );
  }

  isAuthenticated(): boolean {
    return this.userSubject.value !== null;
  }

  getCurrentUser(): User | null {
    return this.userSubject.value;
  }

  logout(): Observable<void> {
    return this.http.post<{success: boolean}>(`${this.apiBase}/auth/logout`, {}, {
      withCredentials: true // 🔥 This sends HttpOnly cookies
    }).pipe(
      tap(() => this.userSubject.next(null)),
      map(() => void 0),
      catchError(error => {
        console.error('[AuthService] Logout failed:', error);
        this.userSubject.next(null);
        return of(void 0);
      })
    );
  }

  login(
    email: string,
    password: string,
    loginUrl: string,
    redirectUrl: string,
  ) {
    //TODO: KSY encrypt token before storage

    return this.http.post<AuthResponse>(loginUrl, { email, password },  {
    withCredentials: true // Add this - browser will save cookies from response
  }).pipe(
      take(1),
      tap((res) => {
        //const token = res.accessToken ?? res.AccessToken ?? '';
        //this.setToken(token);
       // this.redirect(redirectUrl);
       console.log('[AuthService] Login response:', res);
      }),
      map(() => true),
      catchError(() => of(false)),
    );
  }
}

interface AuthResponse {
  accessToken?: string;
  AccessToken?: string;
  expiresAt?: string | Date;
  tokenType?: string;
}
