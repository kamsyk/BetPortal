import { CanActivateFn, Router } from '@angular/router';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from 'ks-ag-controls';
import { SsrCookieService } from 'ngx-cookie-service-ssr';
import { AUTH_SERVER_TOKEN } from '../../core/tokens';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);
  const cookieService = inject(SsrCookieService);

  const toLogin = router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url || '/' },
  });

  // // SSR-safe: no dialogs; just redirect
  // if (!isPlatformBrowser(platformId)) {
  //   //return true;

  //   const token = cookieService.get('auth_token');
  //    if(token) return true;

  //   const serverToken = inject(AUTH_SERVER_TOKEN, { optional: true });
  //   return serverToken ? true : toLogin;

  //   // Uncomment below to enforce auth on SSR too
  //   // return auth.isAuthenticated() ? true : toLogin;
  // }
console.log('Auth Guard Invoked. Platform:', isPlatformBrowser(platformId) ? 'BROWSER' : 'SSR');
const allCookies = cookieService.getAll();
    console.log('[SSR Guard] All cookies:', Object.keys(allCookies));
    const serverToken = inject(AUTH_SERVER_TOKEN, { optional: true });
    console.log('[SSR Guard] Token from AUTH_SERVER_TOKEN:', serverToken ? 'EXISTS' : 'NULL');
const token = cookieService.get('auth_token');
    console.log('[SSR Guard] Token from SsrCookieService:', token ? 'EXISTS' : 'NULL');


  if (!isPlatformBrowser(platformId)) {
    console.log('[SSR Guard] Checking authentication');

    // Check via SsrCookieService
    const token = cookieService.get('auth_token');
    console.log('[SSR Guard] Token from SsrCookieService:', token ? 'EXISTS' : 'NULL');

    // Check via AUTH_SERVER_TOKEN
    const serverToken = inject(AUTH_SERVER_TOKEN, { optional: true });
    console.log('[SSR Guard] Token from AUTH_SERVER_TOKEN:', serverToken ? 'EXISTS' : 'NULL');

    // Check all cookies
    const allCookies = cookieService.getAll();
    console.log('[SSR Guard] All cookies:', Object.keys(allCookies));

    if (token || serverToken || allCookies) return true;
    return toLogin;
  }

  // Browser: if not authenticated, redirect
  return auth.isAuthenticated() ? true : toLogin;
};
