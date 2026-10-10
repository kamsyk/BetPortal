import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  {
    path: 'sign-up-confirmation',
    loadComponent: () =>
      import('./pages/sign-up-confirmation/sign-up-confirmation.component').then((m) => m.SignUpConfirmationComponent),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    loadComponent: () =>
      import('./pages/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'advisor',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/advisor/advisor.component').then((m) => m.AdvisorComponent),
  },
  { path: '**', redirectTo: '' },
];
