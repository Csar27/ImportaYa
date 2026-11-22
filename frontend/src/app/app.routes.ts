import { Routes } from '@angular/router';

import { adminGuard, authGuard } from './core/auth.guard';
import { Shell } from './features/shell/shell';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'imports' },
  {
    path: 'login',
    title: 'ImportaYa · Iniciar sesión',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      {
        path: 'imports',
        title: 'ImportaYa · Importar usuarios',
        loadComponent: () => import('./features/imports/imports').then((m) => m.Imports),
      },
      {
        path: 'accounts',
        title: 'ImportaYa · Usuarios y roles',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/accounts/accounts').then((m) => m.Accounts),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
