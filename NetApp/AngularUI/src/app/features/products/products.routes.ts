import { Routes } from '@angular/router';
import { ProductStore } from './data-access/product.store';
// `import type` is erased at compile time, so the form's code stays in its own lazy chunk.
import type { ProductForm } from './product-form/product-form';

/**
 * Feature routes, LAZY-LOADED from app.routes.ts.
 *
 * LAZY LOADING: none of this code is in the initial JavaScript bundle. The browser
 * downloads this feature's chunk only when the user first navigates to /products.
 * In a real app with 20 features, that keeps first paint fast.
 *
 * ROUTE-LEVEL PROVIDERS: ProductStore is provided here, not in root, so it is
 * created only when the products feature is used and is shared by the list and form
 * pages. That's how the form can call store.reload() after a save.
 */
export const PRODUCT_ROUTES: Routes = [
  {
    path: '',
    providers: [ProductStore],
    children: [
      {
        path: '',
        title: 'Products',
        loadComponent: () => import('./product-list/product-list').then((m) => m.ProductList),
      },
      {
        path: 'new',
        title: 'New product',
        loadComponent: () => import('./product-form/product-form').then((m) => m.ProductForm),
        // Functional guard: ask before discarding unsaved edits.
        canDeactivate: [(form: ProductForm) => form.canLeave()],
      },
      {
        path: ':id/edit',
        title: 'Edit product',
        loadComponent: () => import('./product-form/product-form').then((m) => m.ProductForm),
        canDeactivate: [(form: ProductForm) => form.canLeave()],
      },
    ],
  },
];
