import { HttpErrorResponse } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Injectable, Injector, PLATFORM_ID, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class ErrorNotificationService {
  private readonly injector = inject(Injector);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  notify(error: HttpErrorResponse): void {
    if (!this.isBrowser) {
      return;
    }

    let message: string;
    if (error.status === 0) {
      message = 'Unable to reach the server. Check your connection and try again.';
    } else if (error.status === 403) {
      message = 'You do not have permission to perform this action.';
    } else if (error.status >= 500) {
      message = 'Something went wrong on our side. Please try again later.';
      const errorId = this.errorId(error.error);
      if (errorId) {
        message += `\nError reference: ${errorId}`;
      }
    } else {
      return;
    }

    this.injector.get(MatSnackBar).open(message, undefined, {
      duration: 7000,
      panelClass: ['http-error-snackbar'],
    });
  }

  private errorId(body: unknown): string | null {
    if (typeof body !== 'object' || body === null || !('errorId' in body)) {
      return null;
    }

    const errorId = body.errorId;
    return typeof errorId === 'string' && errorId.length > 0 ? errorId : null;
  }
}
