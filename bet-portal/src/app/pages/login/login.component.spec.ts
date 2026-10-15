import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthService, KsLoginComponent, KsSignUpComponent } from 'ks-ag-controls';
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

  it('shows the account-required message and preserves the destination after Google rejects an unknown account', () => {
    queryParams = { googleError: 'account-required', returnUrl: '/advisor' };
    component.ngOnInit();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.googleAccountRequired');
    expect(loginControl().redirectUrl()).toBe('/advisor');
    expect(loginControl().googleReturnUrl()).toBe('/advisor');
  });

  it('does not show an account-required message on normal sign-in', () => {
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
  });

  it('opens the existing Sign up dialog after Google returns an unknown account', async () => {
    fixture.destroy();
    queryParams = { googleError: 'account-required', returnUrl: '/advisor' };
    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    TestBed.tick();
    await fixture.whenStable();
    const dialogs = TestBed.inject(MatDialog).openDialogs;
    expect(dialogs).toHaveLength(1);
    const dialog = dialogs[0];
    expect(dialog.componentInstance).toBeInstanceOf(KsSignUpComponent);
    const signUp: KsSignUpComponent = dialog.componentInstance;
    expect(signUp.language()).toBe('cs');
    expect(signUp.googleReturnUrl()).toBe('/advisor');
    expect(signUp.googleAuthUrl()).toBe('http://localhost:5026/api/auth/google/register');
    expect(signUp.facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook/register');
    expect(signUp.facebookReturnUrl()).toBe('/advisor');
    fixture.detectChanges();
    TestBed.tick();
    expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(1);
  });

  it('does not automatically open Sign up on a normal login visit', async () => {
    TestBed.tick();
    await fixture.whenStable();
    expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
  });

  it('uses the registration endpoint when Sign up is opened manually without changing Google login', async () => {
    loginControl().openSignUp();
    await fixture.whenStable();
    const signUp: KsSignUpComponent = TestBed.inject(MatDialog).openDialogs[0].componentInstance;
    expect(signUp.googleAuthUrl()).toBe('http://localhost:5026/api/auth/google/register');
    expect(loginControl().googleAuthUrl()).toBe('http://localhost:5026/api/auth/google');
    expect(signUp.facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook/register');
    expect(loginControl().facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook');
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

  it('opens Sign up after Facebook rejects an unknown account and preserves the return URL', async () => {
    fixture.destroy();
    queryParams = { facebookError: 'account-required', returnUrl: '/advisor' };
    fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    TestBed.tick();
    await fixture.whenStable();
    const signUp: KsSignUpComponent = TestBed.inject(MatDialog).openDialogs[0].componentInstance;
    expect(signUp.facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook/register');
    expect(signUp.facebookReturnUrl()).toBe('/advisor');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.facebookAccountRequired');
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
