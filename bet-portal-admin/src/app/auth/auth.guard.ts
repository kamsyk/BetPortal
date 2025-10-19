import { CanActivateFn, Router } from '@angular/router';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from '../services/auth.service';

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
  /*const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  const toLogin = router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url || '/' },
  });

  // SSR: no dialogs — redirect to /login
  if (!isPlatformBrowser(platformId)) return toLogin;

  // Now safe to check auth (uses localStorage only in browser)
  if (auth.isAuthenticated()) return true;

   const dialog = inject(MatDialog);

   const ref = dialog.open(KsLoginComponent, { disableClose: true, width: '360px' });

  return ref.afterClosed().pipe(map(ok => !!ok && auth.isAuthenticated()));*/
};
