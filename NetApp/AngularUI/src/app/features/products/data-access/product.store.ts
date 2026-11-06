import { Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, switchMap, tap } from 'rxjs';
import { toProblemDetails } from '../../../core/models/problem-details';
import { DEFAULT_PRODUCT_QUERY, PagedResult, Product, ProductQuery, ProductSortField } from './product.models';
import { ProductApiService } from './product-api.service';

interface ProductListState {
  result: PagedResult<Product> | null;
  loading: boolean;
  error: string | null;
}

/**
 * STATE MANAGEMENT for the product list: a lightweight signal "store".
 *
 * Pattern: components READ signals and CALL methods; they never mutate state directly.
 * That gives unidirectional data flow, the core idea of NgRx/Redux, without the
 * boilerplate. (For large apps with many shared stores, NgRx SignalStore formalises this.)
 *
 *   component ──setSearch()/setPage()──▶ query signal
 *                                           │ toObservable
 *                                           ▼
 *                             switchMap → HTTP GET /products
 *                                           │
 *   component ◀── products()/loading() ◀── state signal
 *
 * WHY switchMap? If the user changes page twice quickly, switchMap UNSUBSCRIBES from
 * the first request (HttpClient aborts it) and keeps only the latest. Without it, a slow
 * first response could arrive AFTER the second and overwrite it: a race condition.
 *
 * SCOPE: provided in products.routes.ts (not root). It lives while the products feature
 * is in use, and the list keeps its page/search/sort when you go to Edit and come back.
 */
@Injectable()
export class ProductStore {
  private readonly api = inject(ProductApiService);

  // ---- private writable state ----
  private readonly query = signal<ProductQuery>(DEFAULT_PRODUCT_QUERY);
  private readonly reloadTick = signal(0);
  private readonly state = signal<ProductListState>({ result: null, loading: false, error: null });

  // ---- public read-only selectors (computed = memoised derived state) ----
  readonly currentQuery = this.query.asReadonly();
  readonly products = computed(() => this.state().result?.items ?? []);
  readonly page = computed(() => this.state().result?.page ?? 1);
  readonly totalPages = computed(() => this.state().result?.totalPages ?? 0);
  readonly totalCount = computed(() => this.state().result?.totalCount ?? 0);
  readonly loading = computed(() => this.state().loading);
  readonly error = computed(() => this.state().error);
  readonly isEmpty = computed(() => !this.state().loading && this.state().result?.totalCount === 0);

  constructor() {
    // A combined signal: changes whenever the query OR the reload tick changes.
    const request = computed(() => ({ query: this.query(), tick: this.reloadTick() }));

    toObservable(request)
      .pipe(
        tap(() => this.patch({ loading: true, error: null })),
        switchMap(({ query }) =>
          this.api.search(query).pipe(
            // Handle errors INSIDE switchMap, so one failed request doesn't kill the outer stream.
            catchError((err: unknown) => {
              this.patch({ loading: false, error: toProblemDetails(err).title ?? 'Failed to load products' });
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(), // auto-unsubscribe when the store is destroyed (no memory leaks)
      )
      .subscribe((result) => this.patch({ result, loading: false }));
  }

  // ---- commands (the only way to change state) ----

  setSearch(search: string): void {
    // Any filter change resets to page 1, otherwise you could land on an empty page 7.
    this.query.update((q) => ({ ...q, search, page: 1 }));
  }

  setCategory(categoryId: number | null): void {
    this.query.update((q) => ({ ...q, categoryId, page: 1 }));
  }

  setIncludeDiscontinued(includeDiscontinued: boolean): void {
    this.query.update((q) => ({ ...q, includeDiscontinued, page: 1 }));
  }

  setPage(page: number): void {
    this.query.update((q) => ({ ...q, page }));
  }

  setPageSize(pageSize: number): void {
    this.query.update((q) => ({ ...q, pageSize, page: 1 }));
  }

  /** Clicking the active column flips the direction; a new column starts ascending. */
  toggleSort(field: ProductSortField): void {
    this.query.update((q) => ({
      ...q,
      sortBy: field,
      sortDir: q.sortBy === field && q.sortDir === 'asc' ? 'desc' : 'asc',
      page: 1,
    }));
  }

  /** Re-fetch with the same query (after create/update/delete). */
  reload(): void {
    this.reloadTick.update((n) => n + 1);
  }

  /**
   * After a delete, if we removed the last row on the last page, step back a page
   * rather than showing an empty page.
   */
  afterDelete(): void {
    if (this.products().length === 1 && this.page() > 1) {
      this.setPage(this.page() - 1);
    } else {
      this.reload();
    }
  }

  private patch(partial: Partial<ProductListState>): void {
    this.state.update((s) => ({ ...s, ...partial }));
  }
}
