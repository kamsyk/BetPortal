import { HttpRequest, HttpResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';

import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  async function capture(original: HttpRequest<unknown>): Promise<HttpRequest<unknown>> {
    let forwarded: HttpRequest<unknown> | undefined;

    await firstValueFrom(
      TestBed.runInInjectionContext(() =>
        authInterceptor(original, (request) => {
          forwarded = request;
          return of(new HttpResponse({ status: 200 }));
        }),
      ),
    );

    if (!forwarded) {
      throw new Error('interceptor did not forward the request');
    }
    return forwarded;
  }

  it('attaches cookies and a json content type on login posts', async () => {
    const original = new HttpRequest('POST', 'https://api.test/auth/login', {
      email: 'a',
    });

    const forwarded = await capture(original);

    expect(forwarded).not.toBe(original);
    expect(forwarded.withCredentials).toBeTrue();
    expect(forwarded.headers.get('Content-Type')).toBe('application/json');
    expect(forwarded.body).toEqual({ email: 'a' });
  });

  it('attaches cookies on session reads as well', async () => {
    const forwarded = await capture(
      new HttpRequest('GET', 'https://api.test/auth/me'),
    );

    expect(forwarded.method).toBe('GET');
    expect(forwarded.withCredentials).toBeTrue();
    expect(forwarded.headers.get('Content-Type')).toBe('application/json');
  });
});
