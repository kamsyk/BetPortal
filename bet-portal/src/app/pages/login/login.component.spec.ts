import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthService, KsLoginComponent } from 'ks-ag-controls';
import { MatDialog } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { LoginComponent } from './login.component';
import { API_BASE_URL } from '../../../core/tokens';
import { AppLanguage, LanguageService } from '../../services/language.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  const currentLanguage = signal<AppLanguage>('cs');
  let queryParams: Record<string, string>;

  function loginControl(): KsLoginComponent {
    return fixture.debugElement.query(By.directive(KsLoginComponent)).componentInstance;
  }

  beforeEach(async () => {
    currentLanguage.set('cs');
    queryParams = {};
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([
          { path: 'login', component: LoginComponent },
          { path: 'sign-up', component: LoginComponent },
        ]),
        provideNoopAnimations(),
        provideTranslateService(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { get queryParamMap() { return convertToParamMap(queryParams); } } },
        },
        { provide: API_BASE_URL, useValue: 'http://localhost:5026/api' },
        { provide: AuthService, useValue: { checkAuthStatus: () => of({ isAuthenticated: false }) } },
        { provide: LanguageService, useValue: { currentLanguage } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    TestBed.inject(MatDialog).closeAll();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows the account-exists message and preserves the destination after Google rejects a duplicate account', () => {
    queryParams = { googleError: 'account-exists', returnUrl: '/advisor' };
    component.ngOnInit();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.googleAccountExists');
    expect(loginControl().redirectUrl()).toBe('/advisor');
    expect(loginControl().googleReturnUrl()).toBe('/advisor');
  });

  it('does not show an account-required message on normal sign-in', () => {
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
  });

  it('redirects to the sign-up page after Google returns an unknown account', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.destroy();
    queryParams = { googleError: 'account-required', returnUrl: '/advisor' };
    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(navigateSpy).toHaveBeenCalledWith(['/sign-up'], {
      queryParams: { returnUrl: '/advisor', googleError: 'account-required' },
      replaceUrl: true,
    });
    expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
  });

  it('does not navigate to Sign up on a normal login visit', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');
    await fixture.whenStable();
    expect(navigateSpy).not.toHaveBeenCalledWith(
      ['/sign-up'],
      expect.anything(),
    );
    expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
  });

  it('redirects to the sign-up page when opened with signUp=true', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.destroy();
    queryParams = { signUp: 'true' };
    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(navigateSpy).toHaveBeenCalledWith(['/sign-up'], {
      queryParams: {},
      replaceUrl: true,
    });
  });

  it('routes the library Sign up dialog to the sign-up page', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    loginControl().openSignUp();
    await fixture.whenStable();
    TestBed.tick();
    await fixture.whenStable();
    expect(navigateSpy).toHaveBeenCalledWith(['/sign-up'], {
      queryParams: undefined,
    });
    expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
  });

  it('passes the selected language and preserves login URLs', () => {
    expect(loginControl().language()).toBe('cs');
    expect(loginControl().loginUrl()).toBe('http://localhost:5026/api/auth/login');
    expect(loginControl().redirectUrl()).toBe('/');
    expect(loginControl().googleAuthUrl()).toBe('http://localhost:5026/api/auth/google');
    expect(loginControl().googleReturnUrl()).toBe('/');
    expect(loginControl().facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook');
    expect(loginControl().facebookReturnUrl()).toBe('/');
  });

  it('redirects to Sign up after Facebook rejects an unknown account and preserves the return URL', async () => {
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.destroy();
    queryParams = { facebookError: 'account-required', returnUrl: '/advisor' };
    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(navigateSpy).toHaveBeenCalledWith(['/sign-up'], {
      queryParams: { returnUrl: '/advisor', facebookError: 'account-required' },
      replaceUrl: true,
    });
  });

  it('shows an existing Facebook account message without opening Sign up', async () => {
    queryParams = { facebookError: 'account-exists', returnUrl: '/advisor' };
    component.ngOnInit();
    fixture.detectChanges();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.facebookAccountExists');
    expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
  });

  it('updates the login labels when the selected language changes', async () => {
    for (const [language, heading] of [
      ['cs', 'P\u0159ihl\u00e1sit se'],
      ['de', 'Anmelden'],
      ['en', 'Sign in'],
    ] as const) {
      currentLanguage.set(language);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(loginControl().language()).toBe(language);
      const element: HTMLElement = fixture.nativeElement;
      expect(element.querySelector('h2')?.textContent?.trim()).toBe(heading);
    }
  });
});
