import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
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
    expect(loginControl().providers()).toEqual(['google', 'facebook']);
    expect(loginControl().loginUrl()).toBe('http://localhost:5026/api/auth/login');
    expect(loginControl().redirectUrl()).toBe('/');
    expect(loginControl().googleAuthUrl()).toBe('http://localhost:5026/api/auth/google');
    expect(loginControl().googleReturnUrl()).toBe('/');
    expect(loginControl().facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook');
    expect(loginControl().facebookReturnUrl()).toBe('/');
  });

  it('opens the sign-up dialog after Facebook rejects an unknown account and preserves the return URL', () => {
    queryParams = { facebookError: 'account-required', returnUrl: '/advisor' };
    component.ngOnInit();
    expect(openSignUp).toHaveBeenCalledWith({
      returnUrl: '/advisor',
      googleError: null,
      facebookError: 'account-required',
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
