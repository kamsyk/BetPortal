import { CanActivateFn, Router } from '@angular/router';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from 'ks-ag-controls';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  const toLogin = router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url || '/' },
  });

  // SSR-safe: no dialogs; just redirect
  if (!isPlatformBrowser(platformId)) return toLogin;

  // Browser: if not authenticated, redirect
  return auth.isAuthenticated() ? true : toLogin;
};
