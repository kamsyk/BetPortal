import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideTranslateLoader, provideTranslateService, TranslateLoader } from '@ngx-translate/core';
import { AuthService } from 'ks-ag-controls';
import { SsrCookieService } from 'ngx-cookie-service-ssr';
import { of } from 'rxjs';
import { AppComponent } from './app.component';
import { authInterceptor } from './auth/auth.interceptor';
import { LanguageService } from './services/language.service';
import { ThemeService } from './services/theme.service';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation() {
    return of({});
  }
}

describe('AppComponent session restoration', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideTranslateService({ loader: provideTranslateLoader(FakeTranslateLoader) }),
        { provide: SsrCookieService, useValue: {} },
        {
          provide: LanguageService,
          useValue: { init: vi.fn(), languages: ['en'], currentLanguage: signal('en') },
        },
        { provide: ThemeService, useValue: { isDarkMode: signal(false) } },
      ],
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('restores authenticated navigation on startup without visiting Login', async () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    TestBed.tick();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.primary-nav')).toBeNull();

    const check = http.expectOne('http://localhost:5026/api/auth/check');
    expect(check.request.withCredentials).toBe(true);
    check.flush({ isAuthenticated: true });
    http.expectOne('http://localhost:5026/api/auth/me')
      .flush({ username: 'synthetic-user', fullName: 'Synthetic User', isAuthenticated: true });
    await fixture.whenStable();

    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(true);
    expect(element.querySelectorAll('.primary-nav a')).toHaveLength(3);
    expect(element.querySelector('.theme-button')).not.toBeNull();
    expect(element.querySelector('.profile-button')).not.toBeNull();
    expect(TestBed.inject(LanguageService).init).toHaveBeenCalledOnce();
    http.expectNone('http://localhost:5026/api/auth/check');
  });

  it('keeps anonymous visitors on the public page without authenticated navigation', async () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne('http://localhost:5026/api/auth/check').flush({ isAuthenticated: false });
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.primary-nav')).toBeNull();
    expect(element.querySelector('.profile-button')).not.toBeNull();
    expect(TestBed.inject(AuthService).isAuthenticated()).toBe(false);
    http.expectNone('http://localhost:5026/api/auth/me');
  });

  it('reports session-check errors and keeps authenticated controls hidden', async () => {
    const error = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    try {
      const fixture = TestBed.createComponent(AppComponent);
      fixture.detectChanges();
      TestBed.tick();
      http.expectOne('http://localhost:5026/api/auth/check')
        .flush({}, { status: 503, statusText: 'Service Unavailable' });
      await fixture.whenStable();

      expect(error).toHaveBeenCalledWith('Auth check error:', expect.anything());
      expect(TestBed.inject(AuthService).isAuthenticated()).toBe(false);
      expect((fixture.nativeElement as HTMLElement).querySelector('.primary-nav')).toBeNull();
      http.expectNone('http://localhost:5026/api/auth/me');
    } finally {
      error.mockRestore();
    }
  });

  it('cancels an unfinished session check when the application is destroyed', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    TestBed.tick();
    const check = http.expectOne('http://localhost:5026/api/auth/check');
    fixture.destroy();
    expect(check.cancelled).toBe(true);
  });
});
