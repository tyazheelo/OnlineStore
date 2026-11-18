import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', children: [] },
  { path: 'account', loadComponent: () => import('./account-page').then(module => module.AccountPage) },
  { path: 'admin', loadComponent: () => import('./admin-support-page').then(module => module.AdminSupportPage) },
  { path: '**', redirectTo: '' },
];
