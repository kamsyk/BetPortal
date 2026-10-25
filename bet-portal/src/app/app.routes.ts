import { RedirectFunction, Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

function authDialogRedirect(dialog: 'login' | 'sign-up'): RedirectFunction {
  return ({ queryParams }) => {
    const search = new URLSearchParams({ dialog });
    for (const key of ['returnUrl', 'googleError', 'facebookError', 'microsoftError', 'errorId', 'errorDetails', 'signUp']) {
      const value = queryParams[key];
      const text = Array.isArray(value) ? value[0] : value;
      if (text) {
        search.set(key, text);
      }
    }

    return `/?${search.toString()}`;
  };
}

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
    redirectTo: authDialogRedirect('sign-up'),
  },
  {
    path: 'login',
    redirectTo: authDialogRedirect('login'),
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
