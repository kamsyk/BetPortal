import { HttpErrorResponse } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Injectable, Injector, PLATFORM_ID, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';

@Injectable({ providedIn: 'root' })
export class ErrorNotificationService {
  private readonly injector = inject(Injector);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  notify(error: HttpErrorResponse): void {
    if (!this.isBrowser) {
      return;
    }

    const messageKey = this.messageKey(error.status);
    if (!messageKey) {
      return;
    }

    const translate = this.injector.get(TranslateService);
    let message = translate.instant(messageKey);
    const errorId = this.errorId(error.error);
    if (error.status >= 500 && errorId) {
      message += `\n${translate.instant('httpErrors.reference', { errorId })}`;
    }

    this.injector.get(MatSnackBar).open(message, undefined, {
      duration: 7000,
      panelClass: ['http-error-snackbar'],
    });
  }

  private messageKey(status: number): string | null {
    if (status === 0) {
      return 'httpErrors.connection';
    }

    if (status === 403) {
      return 'httpErrors.forbidden';
    }

    return status >= 500 ? 'httpErrors.server' : null;
  }

  private errorId(body: unknown): string | null {
    if (typeof body !== 'object' || body === null || !('errorId' in body)) {
      return null;
    }

    const errorId = body.errorId;
    return typeof errorId === 'string' && errorId.length > 0 ? errorId : null;
  }
}
