import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';

import { API_BASE_URL } from '../../../core/tokens';
import { AuthService } from '../../services/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let login: jasmine.Spy;

  async function create(returnUrl: string | null): Promise<void> {
    login = jasmine.createSpy('login').and.returnValue(of(true));
    const params: Record<string, string> = {};
    if (returnUrl !== null) {
      params['returnUrl'] = returnUrl;
    }

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: API_BASE_URL, useValue: 'https://api.test' },
        { provide: AuthService, useValue: { login } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { queryParamMap: convertToParamMap(params) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('points the login form at the auth login endpoint', async () => {
    await create(null);

    expect(component.loginUrl()).toBe('https://api.test/auth/login');
    expect(component.redirectUrl()).toBe('/');
  });

  it('keeps the return url that the guard stored', async () => {
    await create('/sports-list');

    expect(component.redirectUrl()).toBe('/sports-list');
  });

  it('falls back to home when returnUrl is empty', async () => {
    await create('');

    expect(component.redirectUrl()).toBe('/');
  });

  it('signs in with the configured credentials and does not set an error on success', async () => {
    await create('/sports-list');

    component.onLoginSuccess({ email: 'someone@else.test' });

    expect(login).toHaveBeenCalledOnceWith(
      'test@local',
      'Pass@123',
      'https://api.test/auth/login',
      '/sports-list',
    );
    expect(component.error).toBe('');
    expect(component.loading).toBeFalse();
  });

  it('shows an invalid-credentials error when login is rejected', async () => {
    await create('/');
    login.and.returnValue(of(false));

    component.onLoginSuccess({ email: 'test@local' });

    expect(component.error).toBe('Invalid credentials');
    expect(component.loading).toBeFalse();
  });

  it('shows a generic error when login fails unexpectedly', async () => {
    await create('/');
    login.and.returnValue(throwError(() => new Error('offline')));

    component.onLoginSuccess({ email: 'test@local' });

    expect(component.error).toBe('An error occurred during login');
    expect(component.loading).toBeFalse();
  });
});
