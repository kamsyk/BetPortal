import { CanActivateFn, Router } from '@angular/router';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from 'ks-ag-controls';
import { SsrCookieService } from 'ngx-cookie-service-ssr';
import { AUTH_SERVER_TOKEN } from '../../core/tokens';

interface GlobalWithRequest {
  __request__?: {
    headers?: {
      cookie?: string;
    };
  };
}

// function getCookieOnServer(name: string): string | null {
//   try {
//     // In Node.js context, we need to access the request object differently
//     // This is a workaround - the proper way would be through REQUEST token
//     if (typeof window === 'undefined' && typeof global !== 'undefined') {
//       // Try to get from global context (set by Express middleware)
//       const req = (global as any).__request__;
//       if (req && req.headers && req.headers.cookie) {
//         const cookies = req.headers.cookie.split(';');
//         for (const cookie of cookies) {
//           const [key, value] = cookie.trim().split('=');
//           if (key === name) {
//             return decodeURIComponent(value);
//           }
//         }

function getCookieOnServer(name: string): string | null {
  //return "fsdfsdsfd";
   try {
     if (typeof window === 'undefined' && typeof global !== 'undefined') {

      const req = (global as GlobalWithRequest).__request__;
      console.log('[getCookieOnServer] Request exists:', !!req);
      console.log('[getCookieOnServer] Headers exist:', !!(req && req.headers));
      console.log('[getCookieOnServer] Cookie header:', req?.headers?.cookie || 'NO COOKIE HEADER');

      return "fsdfsdsfd";
  //     if (req && req.headers && req.headers.cookie) {
  //       return "true";
  //       // const cookies = req.headers.cookie.split(';');
  //       // console.log('[getCookieOnServer] Parsed cookies:', cookies.map(c => c.trim().split('=')[0]));

  //       // for (const cookie of cookies) {
  //       //   const [key, value] = cookie.trim().split('=');
  //       //   if (key === name) {
  //       //     const decoded = decodeURIComponent(value);
  //       //     console.log(`[getCookieOnServer] Found ${name}:`, decoded.substring(0, 10) + '...');
  //       //     return decoded;
  //       //   }
  //       // }
  //     }
     }
  } catch (error) {
    console.error('[getCookieOnServer] Error:', error);
  }
  return null;
}

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
// const allCookies = cookieService.getAll();
//     console.log('[SSR Guard] All cookies:', Object.keys(allCookies));
//     const serverToken = inject(AUTH_SERVER_TOKEN, { optional: true });
//     console.log('[SSR Guard] Token from AUTH_SERVER_TOKEN:', serverToken ? 'EXISTS' : 'NULL');
// const token = cookieService.get('auth_token');
//     console.log('[SSR Guard] Token from SsrCookieService:', token ? 'EXISTS' : 'NULL');


  if (!isPlatformBrowser(platformId)) {
    console.log('[SSR Guard] Checking authentication');

    const token = getCookieOnServer('auth_token');
    //console.log('[SSR Guard] Manual cookie parse result:', token ? 'EXISTS' : 'NULL');


    // // Check via SsrCookieService
    // const token = cookieService.get('auth_token');
    // console.log('[SSR Guard] Token from SsrCookieService:', token ? 'EXISTS' : 'NULL');

    // // Check via AUTH_SERVER_TOKEN
    // const serverToken = inject(AUTH_SERVER_TOKEN, { optional: true });
    // console.log('[SSR Guard] Token from AUTH_SERVER_TOKEN:', serverToken ? 'EXISTS' : 'NULL');

    // // Check all cookies
    // const allCookies = cookieService.getAll();
    // console.log('[SSR Guard] All cookies:', Object.keys(allCookies));

    // console.log('[SSR Guard] Authentication result:', (token || serverToken) ? 'AUTHENTICATED' : 'NOT AUTHENTICATED');

   // if (token || serverToken) return true;

   if (token) return true;



  }

  // Browser: if not authenticated, redirect
  //return auth.isAuthenticated() ? true : toLogin;

  const tokenBrowser = cookieService.get('auth_token');
  if (tokenBrowser) return true;

  console.log('Login url:', toLogin);
  return toLogin;
};
