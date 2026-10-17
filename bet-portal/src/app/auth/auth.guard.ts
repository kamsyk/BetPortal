import { isPlatformBrowser } from '@angular/common';
import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from 'ks-ag-controls';
import { map, catchError, of } from 'rxjs';
import { AuthDialogService } from '../services/auth-dialog.service';


export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const authDialog = inject(AuthDialogService);
  const isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  const openLogin = (): boolean | UrlTree => {
    if (!isBrowser || !router.navigated) {
      return router.createUrlTree(['/'], {
        queryParams: { dialog: 'login', returnUrl: state.url },
      });
    }

    authDialog.openLogin({ returnUrl: state.url });
    return false;
  };
  // const platformId = inject(PLATFORM_ID);
  // const cookieService = inject(SsrCookieService);

  // First check the current authentication status
  // First check the current authentication status
  if (authService.isAuthenticated()) {
    return true;
  }

//   if (!isPlatformBrowser(platformId)) {
//     console.log('[SSR Guard] Checking authentication');

//     const token = cookieService.get('auth_token');
//     console.log('[SSR Guard] Token from SsrCookieService:', token ? 'EXISTS' : 'NULL');




// //  // Get the request object to access cookies
// //     const request = inject(REQUEST, { optional: true });

// //     if (request) {
// //       const cookies = request.headers.cookie || '';
// //       console.log('[SSR Guard] Server cookies:', cookies);

// //       // Extract AuthCookie from request headers
// //       const authCookie = extractCookieValue(cookies, 'AuthCookie');

// //       if (authCookie) {
// //         console.log('[SSR Guard] AuthCookie found on server');
// //         // Pass the cookie to your auth service for server-side validation
// //         return authService.checkAuthStatusWithCookie(authCookie).pipe(
// //           map(response => {
// //             if (response.isAuthenticated) {
// //               return true;
// //             } else {
// //               router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
// //               return false;
// //             }
// //           }),
// //           catchError(() => {
// //             router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
// //             return of(false);
// //           })
// //         );
// //       }
// //     }

// //     // No cookie found on server, redirect to login
// //     router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
// //     return false;

//     return true;
//   }

  // If not authenticated locally, check with the server
  return authService.checkAuthStatus().pipe(
    map(response => {
      if (response.isAuthenticated) {
        return true;
      }

      return openLogin();
    }),
    catchError(() => of(openLogin()))
  );
};
