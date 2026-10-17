import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  {
    path: 'user-token',
    loadComponent: () =>
      import('./pages/user-token/user-token.component').then((m) => m.UserTokenComponent),
  },
  {
    path: 'welcome',
    loadComponent: () =>
      import('./pages/welcome/welcome.component').then((m) => m.WelcomeComponent),
  },
  {
    path: 'sign-up-confirmation',
    loadComponent: () =>
      import('./pages/sign-up-confirmation/sign-up-confirmation.component').then((m) => m.SignUpConfirmationComponent),
  },
  {
    path: 'sign-up',
    loadComponent: () =>
      import('./pages/sign-up/sign-up.component').then((m) => m.SignUpComponent),
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
