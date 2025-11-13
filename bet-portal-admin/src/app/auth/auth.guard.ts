import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from 'ks-ag-controls';
import { map, catchError, of } from 'rxjs';
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  // First check the current authentication status
  // First check the current authentication status
  if (authService.isAuthenticated()) {
    return true;
  }

  if (!isPlatformBrowser(platformId)) {
    console.log('[SSR Guard] Checking authentication');
    return true;
  }

  // If not authenticated locally, check with the server
  return authService.checkAuthStatus().pipe(
    map(response => {
      if (response.isAuthenticated) {
        return true;
      } else {
        router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
        return false;
      }
    }),
    catchError(() => {
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return of(false);
    })
  );
};
