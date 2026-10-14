import { PLATFORM_ID } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { API_BASE_URL } from '../../core/tokens';
import { AuthService } from './auth.service';

const apiBase = 'https://api.test';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  function setup(platform: 'browser' | 'server'): void {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: apiBase },
        { provide: PLATFORM_ID, useValue: platform },
      ],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    http.verify();
  });

  describe('on the server', () => {
    beforeEach(() => {
      setup('server');
    });

    it('does not call the session endpoint while the service is constructed', () => {
      http.expectNone(`${apiBase}/auth/me`);
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('stores the user and reports authenticated when the session is valid', () => {
      let authenticated: boolean | undefined;

      service.validateSession().subscribe((value) => {
        authenticated = value;
      });

      const request = http.expectOne(`${apiBase}/auth/me`);
      expect(request.request.method).toBe('GET');
      request.flush({
        success: true,
        user: { id: 7, email: 'test@local', name: 'test' },
      });

      expect(authenticated).toBeTrue();
      expect(service.isAuthenticated()).toBeTrue();
      expect(service.getCurrentUser()).toEqual({
        id: 7,
        email: 'test@local',
        name: 'test',
      });
    });

    it('clears a previously stored user when the session is no longer valid', () => {
      service.validateSession().subscribe();
      http.expectOne(`${apiBase}/auth/me`).flush({
        success: true,
        user: { id: 7, email: 'test@local', name: 'test' },
      });

      let authenticated: boolean | undefined;
      service.validateSession().subscribe((value) => {
        authenticated = value;
      });
      http.expectOne(`${apiBase}/auth/me`).flush({ success: true, user: null });

      expect(authenticated).toBeFalse();
      expect(service.getCurrentUser()).toBeNull();
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('treats a failed session check as logged out', () => {
      let authenticated: boolean | undefined;

      service.validateSession().subscribe((value) => {
        authenticated = value;
      });
      http
        .expectOne(`${apiBase}/auth/me`)
        .flush(null, { status: 401, statusText: 'Unauthorized' });

      expect(authenticated).toBeFalse();
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('posts credentials with cookies and reports success', () => {
      let succeeded: boolean | undefined;

      service
        .login('test@local', 'Pass@123', `${apiBase}/auth/login`, '/sports-list')
        .subscribe((value) => {
          succeeded = value;
        });

      const request = http.expectOne(`${apiBase}/auth/login`);
      expect(request.request.method).toBe('POST');
      expect(request.request.withCredentials).toBeTrue();
      expect(request.request.body).toEqual({
        email: 'test@local',
        password: 'Pass@123',
      });
      request.flush({ success: true });

      expect(succeeded).toBeTrue();
    });

    it('reports failure when login is rejected and does not throw', () => {
      let succeeded: boolean | undefined;

      service
        .login('', '', `${apiBase}/auth/login`, '/')
        .subscribe((value) => {
          succeeded = value;
        });

      const request = http.expectOne(`${apiBase}/auth/login`);
      expect(request.request.body).toEqual({ email: '', password: '' });
      request.flush(
        { message: 'Invalid credentials' },
        { status: 401, statusText: 'Unauthorized' },
      );

      expect(succeeded).toBeFalse();
    });

    it('clears the user after a successful logout that sends the session cookie', () => {
      service.validateSession().subscribe();
      http.expectOne(`${apiBase}/auth/me`).flush({
        success: true,
        user: { id: 7, email: 'test@local', name: 'test' },
      });

      let completed = false;
      service.logout().subscribe(() => {
        completed = true;
      });

      const request = http.expectOne(`${apiBase}/api/auth/logout`);
      expect(request.request.method).toBe('POST');
      expect(request.request.withCredentials).toBeTrue();
      expect(service.isAuthenticated()).toBeTrue();
      request.flush({ success: true });

      expect(completed).toBeTrue();
      expect(service.getCurrentUser()).toBeNull();
    });

    it('still clears the user when logout fails', () => {
      service.validateSession().subscribe();
      http.expectOne(`${apiBase}/auth/me`).flush({
        success: true,
        user: { id: 7, email: 'test@local', name: 'test' },
      });

      let completed = false;
      service.logout().subscribe(() => {
        completed = true;
      });
      http
        .expectOne(`${apiBase}/api/auth/logout`)
        .flush(null, { status: 500, statusText: 'Server Error' });

      expect(completed).toBeTrue();
      expect(service.isAuthenticated()).toBeFalse();
    });
  });

  describe('in the browser', () => {
    beforeEach(() => {
      setup('browser');
    });

    it('validates the session as soon as the service is created', () => {
      const request = http.expectOne(`${apiBase}/auth/me`);
      request.flush({
        success: true,
        user: { id: 1, email: 'test@local', name: 'test' },
      });

      expect(service.isAuthenticated()).toBeTrue();
    });
  });
});
