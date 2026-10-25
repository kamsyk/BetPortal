import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthService } from 'ks-ag-controls';
import { of } from 'rxjs';

import { routes } from './app.routes';
import { HomeComponent } from './pages/home/home.component';
import { LanguageService } from './services/language.service';
import { API_BASE_URL } from '../core/tokens';

describe('Application routes', () => {
  const authenticated = signal(false);
  const checkAuthStatus = vi.fn<AuthService['checkAuthStatus']>();

  beforeEach(() => {
    authenticated.set(false);
    checkAuthStatus.mockReset();
    checkAuthStatus.mockReturnValue(of({ isAuthenticated: false }));
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideTranslateService(),
        { provide: API_BASE_URL, useValue: 'http://localhost:5026/api' },
        { provide: LanguageService, useValue: { currentLanguage: signal('en') } },
        {
          provide: AuthService,
          useValue: { isAuthenticated: authenticated, checkAuthStatus },
        },
      ],
    });
  });

  for (const isAuthenticated of [false, true]) {
    it(`opens home without checking authentication (authenticated: ${isAuthenticated})`, async () => {
      authenticated.set(isAuthenticated);
      const harness = await RouterTestingHarness.create();
      await harness.navigateByUrl('/', HomeComponent);

      expect(TestBed.inject(Router).url).toBe('/');
      expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('home.title');
      expect(checkAuthStatus).not.toHaveBeenCalled();
    });
  }

  it('still redirects anonymous advisor visitors to login', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/advisor');
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/?dialog=login&returnUrl=%2Fadvisor');
    expect(checkAuthStatus).toHaveBeenCalled();
  });

  it('keeps an existing login link on the home page and preserves its alert', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/login?googleError=account-exists&returnUrl=%2Fadvisor', HomeComponent);

    const loginUrl = new URL(TestBed.inject(Router).url, 'http://localhost');
    expect(loginUrl.searchParams.get('dialog')).toBe('login');
    expect(loginUrl.searchParams.get('googleError')).toBe('account-exists');
    expect(loginUrl.searchParams.get('returnUrl')).toBe('/advisor');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('home.title');
  });

  it('preserves Microsoft callback errors and destination when opening the login dialog', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/login?microsoftError=account-not-linked&returnUrl=%2Fadvisor&errorId=0123456789abcdef0123456789abcdef', HomeComponent);
    const url = new URL(TestBed.inject(Router).url, 'http://localhost');
    expect(url.searchParams.get('dialog')).toBe('login');
    expect(url.searchParams.get('microsoftError')).toBe('account-not-linked');
    expect(url.searchParams.get('returnUrl')).toBe('/advisor');
    expect(url.searchParams.get('errorId')).toBe('0123456789abcdef0123456789abcdef');
  });

  it('keeps an existing sign-up link on the home page', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/sign-up?returnUrl=%2Fwelcome', HomeComponent);

    expect(TestBed.inject(Router).url).toBe('/?dialog=sign-up&returnUrl=%2Fwelcome');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toContain('home.title');
  });
});
