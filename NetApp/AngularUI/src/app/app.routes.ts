import { Routes } from '@angular/router';

/**
 * Top-level routes. Each feature is lazy-loaded with `loadChildren`, so the
 * initial bundle contains only the app shell.
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'products' },
  {
    path: 'products',
    loadChildren: () => import('./features/products/products.routes').then((m) => m.PRODUCT_ROUTES),
  },
  // Catch-all: unknown URLs go back to the list rather than a blank page.
  { path: '**', redirectTo: 'products' },
];
