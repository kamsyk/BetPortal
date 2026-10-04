import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from 'ks-ag-controls';
import { of } from 'rxjs';
import { API_BASE_URL } from './core/tokens';

export default [
  provideHttpClient(),
  provideHttpClientTesting(),
  provideRouter([]),
  { provide: API_BASE_URL, useValue: 'http://localhost:5026/api' },
  {
    provide: AuthService,
    useValue: {
      checkAuthStatus: () => of({ isAuthenticated: false }),
      isAuthenticated: () => false,
      logout: () => of({ success: true, message: '' }),
    },
  },
];
