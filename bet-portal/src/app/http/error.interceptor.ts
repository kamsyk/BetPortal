import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ErrorNotificationService } from './error-notification.service';

export const SKIP_ERROR_NOTIFICATION = new HttpContextToken<boolean>(() => false);

function isAuthEndpoint(url: string): boolean {
  const path = url.split(/[?#]/, 1)[0];
  return /\/auth\/(?:check|login)\/?$/i.test(path);
}

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const notifications = inject(ErrorNotificationService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse
        && !request.context.get(SKIP_ERROR_NOTIFICATION)
        && !isAuthEndpoint(request.url)
      ) {
        notifications.notify(error);
      }

      return throwError(() => error);
    }),
  );
};
