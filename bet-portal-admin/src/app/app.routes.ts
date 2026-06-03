import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'sports-list',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/sports/sports-list/sports-list.component').then((m) => m.SportsListComponent),
  },
  { path: '**', redirectTo: '' },
];
