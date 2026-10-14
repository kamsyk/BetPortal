import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  GuardResult,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { firstValueFrom, isObservable, Observable, of, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let validateSession: jasmine.Spy<() => Observable<boolean>>;

  function setup(platform: 'browser' | 'server'): void {
    validateSession = jasmine.createSpy('validateSession');
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { validateSession } },
        { provide: PLATFORM_ID, useValue: platform },
      ],
    });
  }

  async function activate(url: string): Promise<GuardResult> {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );
    if (isObservable(result)) {
      return firstValueFrom(result);
    }
    return result;
  }

  it('allows activation during server rendering without calling the session API', async () => {
    setup('server');

    const result = await activate('/sports-list');

    expect(result).toBeTrue();
    expect(validateSession).not.toHaveBeenCalled();
  });

  it('allows activation in the browser when the session is valid', async () => {
    setup('browser');
    validateSession.and.returnValue(of(true));

    const result = await activate('/sports-list');

    expect(result).toBeTrue();
    expect(validateSession).toHaveBeenCalledTimes(1);
  });

  it('redirects to login and keeps the attempted url when the session is invalid', async () => {
    setup('browser');
    validateSession.and.returnValue(of(false));

    const result = await activate('/sports-list');
    const router = TestBed.inject(Router);

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe(
      '/login?returnUrl=%2Fsports-list',
    );
  });

  it('uses the home url when the attempted navigation has an empty url', async () => {
    setup('browser');
    validateSession.and.returnValue(of(false));

    const result = await activate('');
    const router = TestBed.inject(Router);

    expect(router.serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2F');
  });

  it('redirects to login when session validation errors', async () => {
    setup('browser');
    validateSession.and.returnValue(throwError(() => new Error('network')));

    const result = await activate('/sports-list');
    const router = TestBed.inject(Router);

    expect(router.serializeUrl(result as UrlTree)).toBe(
      '/login?returnUrl=%2Fsports-list',
    );
  });
});
