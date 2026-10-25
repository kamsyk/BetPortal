import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import { AuthService, KsLoginComponent, KsSignUpComponent } from 'ks-ag-controls';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { LoginComponent } from './login.component';
import { API_BASE_URL } from '../../../core/tokens';
import { AppLanguage, LanguageService } from '../../services/language.service';
import { AuthDialogService } from '../../services/auth-dialog.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  const currentLanguage = signal<AppLanguage>('cs');
  const openSignUp = vi.fn<AuthDialogService['openSignUp']>();
  const dialogClose = vi.fn();
  let queryParams: Record<string, string>;

  function loginControl(): KsLoginComponent {
    return fixture.debugElement.query(By.directive(KsLoginComponent)).componentInstance;
  }

  function signUpDialogCount(): number {
    return TestBed.inject(MatDialog).openDialogs
      .filter((dialog) => dialog.componentInstance instanceof KsSignUpComponent)
      .length;
  }

  beforeEach(async () => {
    currentLanguage.set('cs');
    queryParams = {};
    openSignUp.mockClear();
    dialogClose.mockClear();
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideTranslateService(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { get queryParamMap() { return convertToParamMap(queryParams); } } },
        },
        { provide: API_BASE_URL, useValue: 'http://localhost:5026/api' },
        { provide: AuthService, useValue: { checkAuthStatus: () => of({ isAuthenticated: false }) } },
        { provide: LanguageService, useValue: { currentLanguage } },
        { provide: AuthDialogService, useValue: { openSignUp, openLogin: vi.fn() } },
        { provide: MatDialogRef, useValue: { close: dialogClose, id: 'login-dialog' } },
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

  it('closes the dialog without leaving the current page', () => {
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    expect((fixture.nativeElement as HTMLElement).querySelector('lib-ks-login')).not.toBeNull();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.login-dialog-close')?.click();
    expect(dialogClose).toHaveBeenCalled();
    expect(navigateSpy).not.toHaveBeenCalled();
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
    expect(openSignUp).not.toHaveBeenCalled();
  });

  it('opens the sign-up dialog after Google returns an unknown account', () => {
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');
    queryParams = { googleError: 'account-required', returnUrl: '/advisor' };
    component.ngOnInit();
    expect(openSignUp).toHaveBeenCalledWith({
      returnUrl: '/advisor',
      googleError: 'account-required',
      facebookError: null,
      microsoftError: null,
    });
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('does not open Sign up on a normal login visit', async () => {
    await fixture.whenStable();
    expect(openSignUp).not.toHaveBeenCalled();
    expect(signUpDialogCount()).toBe(0);
  });

  it('opens the sign-up dialog when opened with signUp=true', () => {
    queryParams = { signUp: 'true' };
    component.ngOnInit();
    expect(openSignUp).toHaveBeenCalledWith({
      returnUrl: '/',
      googleError: null,
      facebookError: null,
      microsoftError: null,
    });
  });

  it('routes the library Sign up dialog to the portal sign-up dialog', async () => {
    loginControl().openSignUp();
    await fixture.whenStable();
    TestBed.tick();
    await fixture.whenStable();
    expect(openSignUp).toHaveBeenCalledWith({ returnUrl: '/' });
    expect(signUpDialogCount()).toBe(0);
  });

  it('passes the selected language and preserves login URLs', () => {
    expect(loginControl().language()).toBe('cs');
    expect(loginControl().providers()).toEqual(['google', 'facebook', 'microsoft']);
    expect(loginControl().loginUrl()).toBe('http://localhost:5026/api/auth/login');
    expect(loginControl().redirectUrl()).toBe('/');
    expect(loginControl().googleAuthUrl()).toBe('http://localhost:5026/api/auth/google');
    expect(loginControl().googleReturnUrl()).toBe('/');
    expect(loginControl().facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook');
    expect(loginControl().facebookReturnUrl()).toBe('/');
    expect(loginControl().microsoftAuthUrl()).toBe('http://localhost:5026/api/auth/microsoft');
    expect(loginControl().microsoftReturnUrl()).toBe('/');
  });

  it('opens the sign-up dialog after Facebook rejects an unknown account and preserves the return URL', () => {
    queryParams = { facebookError: 'account-required', returnUrl: '/advisor' };
    component.ngOnInit();
    expect(openSignUp).toHaveBeenCalledWith({
      returnUrl: '/advisor',
      googleError: null,
      facebookError: 'account-required',
      microsoftError: null,
    });
  });

  it('shows an existing Facebook account message without opening Sign up', async () => {
    queryParams = { facebookError: 'account-exists', returnUrl: '/advisor' };
    component.ngOnInit();
    fixture.detectChanges();
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.facebookAccountExists');
    expect(openSignUp).not.toHaveBeenCalled();
  });

  it('opens sign-up after Microsoft returns an unknown account, preserving its destination', () => {
    queryParams = { microsoftError: 'account-required', returnUrl: '/advisor' };
    component.ngOnInit();
    expect(openSignUp).toHaveBeenCalledWith({
      returnUrl: '/advisor',
      googleError: null,
      facebookError: null,
      microsoftError: 'account-required',
    });
  });

  it('shows Microsoft duplicate-account messaging without opening sign-up', () => {
    queryParams = { microsoftError: 'account-exists', returnUrl: '/advisor' };
    component.ngOnInit();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.microsoftAccountExists');
    expect(loginControl().microsoftReturnUrl()).toBe('/advisor');
    expect(openSignUp).not.toHaveBeenCalled();
  });


  it.each([
    ['account-not-linked', 'Google'],
    ['account-inactive', 'Facebook'],
    ['account-unavailable', 'Microsoft'],
    ['email-unavailable', 'Google'],
    ['sign-in-failed', 'Facebook'],
    ['activation-email-failed', 'Microsoft'],
    ['not-configured', 'Google'],
  ])('renders a localized %s external-login error for %s', (code, provider) => {
    queryParams = { googleError: code };
    if (provider === 'Facebook') {
      queryParams = { facebookError: code };
    } else if (provider === 'Microsoft') {
      queryParams = { microsoftError: code };
    }
    component.ngOnInit();
    expect(component.externalAuthError()).toEqual({
      messageKey: `login.externalErrors.${code}`,
      provider,
    });
  });

  it('localizes an unrecognized external error, interpolates the provider, and shows its reference once', async () => {
    const reference = '0123456789abcdef0123456789abcdef';
    queryParams = { googleError: 'unknown-error', facebookError: 'account-inactive', errorId: reference };
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', {
      login: {
        errorReference: 'Error reference',
        externalErrors: { 'sign-in-failed': 'Sign-in with {{provider}} failed.' },
      },
    });
    await translate.use('en').toPromise();
    component.ngOnInit();
    fixture.detectChanges();
    await fixture.whenStable();

    const alerts = (fixture.nativeElement as HTMLElement).querySelectorAll('[role="alert"]');
    expect(alerts).toHaveLength(1);
    expect(alerts[0].textContent).toContain('Sign-in with Google failed.');
    expect(alerts[0].textContent).toContain('Error reference');
    expect(alerts[0].querySelector('code')?.textContent).toBe(reference);
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
      expect((fixture.nativeElement as HTMLElement).querySelector('h2')?.textContent?.trim()).toBe(heading);
    }
  });
});
