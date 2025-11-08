import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from 'ks-ag-controls';
import { map, catchError, of } from 'rxjs';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // First check the current authentication status
  if (authService.isAuthenticated()) {
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
