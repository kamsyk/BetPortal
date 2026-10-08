import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { AuthService, KsLoginComponent } from 'ks-ag-controls';
import { of } from 'rxjs';

import { LoginComponent } from './login.component';
import { API_BASE_URL } from '../../../core/tokens';
import { AppLanguage, LanguageService } from '../../services/language.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  const currentLanguage = signal<AppLanguage>('cs');

  function loginControl(): KsLoginComponent {
    return fixture.debugElement.query(By.directive(KsLoginComponent)).componentInstance;
  }

  beforeEach(async () => {
    currentLanguage.set('cs');
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        { provide: API_BASE_URL, useValue: 'http://localhost:5026/api' },
        { provide: AuthService, useValue: { checkAuthStatus: () => of({ isAuthenticated: false }) } },
        { provide: LanguageService, useValue: { currentLanguage } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('passes the selected language and preserves login URLs', () => {
    expect(loginControl().language()).toBe('cs');
    expect(loginControl().loginUrl()).toBe('http://localhost:5026/api/auth/login');
    expect(loginControl().redirectUrl()).toBe('/');
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
