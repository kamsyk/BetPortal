import { CanActivateFn, Router } from '@angular/router';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from 'ks-ag-controls';
import { AUTH_SERVER_TOKEN } from '../../core/tokens';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const platformId = inject(PLATFORM_ID);

  const toLogin = router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url || '/' },
  });

  // // SSR-safe: no dialogs; just redirect
  // if (!isPlatformBrowser(platformId)) {
  //   //return auth.isAuthenticated() ? true : toLogin;
  //   return true; // Allow all on server side
  // }

  // SSR: do NOT render protected pages without auth
  if (!isPlatformBrowser(platformId)) {
    const serverToken = inject(AUTH_SERVER_TOKEN, { optional: true });
    if (serverToken) return true;

    // // Optional: send a 302 on the server
    // const res = inject(RESPONSE, { optional: true }) as any;
    // if (res) {
    //   res.statusCode = 302;
    //   res.setHeader('Location', router.serializeUrl(toLogin));
    // }
    return toLogin;
  }

  // Browser: if not authenticated, redirect
  return auth.isAuthenticated() ? true : toLogin;
};
