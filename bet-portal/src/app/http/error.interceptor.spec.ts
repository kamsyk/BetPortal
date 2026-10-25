import { HttpContext, HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';
import { lastValueFrom, throwError } from 'rxjs';

import { ErrorNotificationService } from './error-notification.service';
import { errorInterceptor, SKIP_ERROR_NOTIFICATION } from './error.interceptor';

describe('errorInterceptor', () => {
  const open = vi.fn();
  const instant = vi.fn((key: string, params?: { errorId?: string }) => {
    if (key === 'httpErrors.reference') {
      return `Reference: ${params?.errorId ?? ''}`;
    }
    return ({
      'httpErrors.connection': 'Connection message',
      'httpErrors.server': 'Server message',
      'httpErrors.forbidden': 'Permission message',
    } as Record<string, string>)[key] ?? key;
  });

  beforeEach(() => {
    open.mockClear();
    instant.mockClear();
    TestBed.configureTestingModule({
      providers: [
        ErrorNotificationService,
        { provide: MatSnackBar, useValue: { open } },
        { provide: TranslateService, useValue: { instant } },
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
  });

  async function requestError(
    error: unknown,
    url = 'https://api.example.test/users',
    context = new HttpContext(),
  ): Promise<void> {
    const request = new HttpRequest('GET', url, { context });
    const response = TestBed.runInInjectionContext(() =>
      errorInterceptor(request, () => throwError(() => error)),
    );
    await expect(lastValueFrom(response)).rejects.toBe(error);
  }

  it.each([
    [new HttpErrorResponse({ status: 0 }), 'Connection message'],
    [new HttpErrorResponse({ status: 403 }), 'Permission message'],
    [new HttpErrorResponse({ status: 500 }), 'Server message'],
  ])('shows a friendly message for HTTP status %s and rethrows the original error', async (error, message) => {
    await requestError(error);
    expect(open).toHaveBeenCalledWith(message, undefined, expect.objectContaining({ duration: 7000 }));
  });

  it('includes the backend error reference for server failures', async () => {
    const errorId = '0123456789abcdef0123456789abcdef';
    await requestError(new HttpErrorResponse({ status: 500, error: { errorId } }));
    expect(open.mock.calls[0][0]).toBe(`Server message\nReference: ${errorId}`);
  });

  it.each([400, 401, 404, 409, 422])('does not notify for HTTP status %s', async (status) => {
    await requestError(new HttpErrorResponse({ status }));
    expect(open).not.toHaveBeenCalled();
  });

  it.each(['/api/auth/check', 'https://api.example.test/auth/login?next=/home'])('skips auth endpoint %s', async (url) => {
    await requestError(new HttpErrorResponse({ status: 500 }), url);
    expect(open).not.toHaveBeenCalled();
  });

  it('respects the opt-out context token', async () => {
    const context = new HttpContext().set(SKIP_ERROR_NOTIFICATION, true);
    await requestError(new HttpErrorResponse({ status: 500 }), undefined, context);
    expect(open).not.toHaveBeenCalled();
  });

  it('does not show notifications during server-side rendering', async () => {
    TestBed.overrideProvider(PLATFORM_ID, { useValue: 'server' });
    TestBed.overrideProvider(MatSnackBar, {
      useFactory: () => { throw new Error('MatSnackBar must not be resolved on the server'); },
    });
    await requestError(new HttpErrorResponse({ status: 500 }));
    expect(open).not.toHaveBeenCalled();
  });
});
