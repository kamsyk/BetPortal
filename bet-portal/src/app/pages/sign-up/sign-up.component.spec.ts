import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { AuthService, KsSignUpComponent } from 'ks-ag-controls';
import { of } from 'rxjs';

import { SignUpComponent } from './sign-up.component';
import { API_BASE_URL } from '../../../core/tokens';
import { AppLanguage, LanguageService } from '../../services/language.service';
import { AuthDialogService } from '../../services/auth-dialog.service';

describe('SignUpComponent', () => {
  let component: SignUpComponent;
  let fixture: ComponentFixture<SignUpComponent>;
  const currentLanguage = signal<AppLanguage>('cs');
  const openLogin = vi.fn<AuthDialogService['openLogin']>();
  let queryParams: Record<string, string>;

  function signUpControl(): KsSignUpComponent {
    return fixture.debugElement.query(By.directive(KsSignUpComponent)).componentInstance;
  }

  beforeEach(async () => {
    currentLanguage.set('cs');
    queryParams = {};
    openLogin.mockClear();
    await TestBed.configureTestingModule({
      imports: [SignUpComponent],
      providers: [
        provideRouter([]),
        provideTranslateService(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { get queryParamMap() { return convertToParamMap(queryParams); } } },
        },
        { provide: API_BASE_URL, useValue: 'http://localhost:5026/api' },
        { provide: AuthService, useValue: { checkAuthStatus: () => of({ isAuthenticated: false }) } },
        { provide: LanguageService, useValue: { currentLanguage } },
        { provide: AuthDialogService, useValue: { openLogin, openSignUp: vi.fn() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SignUpComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('renders the sign-up form for the dialog with registration endpoints', () => {
    expect(signUpControl().language()).toBe('cs');
    expect(signUpControl().googleAuthUrl()).toBe('http://localhost:5026/api/auth/google/register');
    expect(signUpControl().facebookAuthUrl()).toBe('http://localhost:5026/api/auth/facebook/register');
    expect(signUpControl().googleReturnUrl()).toBe('/');
    expect(signUpControl().facebookReturnUrl()).toBe('/');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="dialog"]')).toBeNull();
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.sign-in-prompt button')?.click();
    expect(openLogin).toHaveBeenCalledWith({ returnUrl: '/' });
  });

  it('preserves the return URL and shows Google account-required messaging', () => {
    queryParams = { googleError: 'account-required', returnUrl: '/advisor' };
    component.ngOnInit();
    fixture.detectChanges();

    expect(signUpControl().googleReturnUrl()).toBe('/advisor');
    expect(signUpControl().facebookReturnUrl()).toBe('/advisor');
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.googleAccountRequired');
  });

  it('shows Facebook account-required messaging', () => {
    queryParams = { facebookError: 'account-required', returnUrl: '/advisor' };
    component.ngOnInit();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('login.facebookAccountRequired');
  });

  it('updates labels when the selected language changes', async () => {
    for (const [language, heading] of [
      ['cs', 'Zaregistrovat se'],
      ['de', 'Registrieren'],
      ['en', 'Sign Up'],
    ] as const) {
      currentLanguage.set(language);
      fixture.detectChanges();
      await fixture.whenStable();

      expect(signUpControl().language()).toBe(language);
      expect((fixture.nativeElement as HTMLElement).querySelector('h2')?.textContent?.trim()).toBe(heading);
    }
  });
});
