import { InjectionToken } from '@angular/core';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
export const AUTH_SERVER_TOKEN = new InjectionToken<string | null>('AUTH_SERVER_TOKEN');
