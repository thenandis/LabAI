import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../../../core/api.config';
import { CreateProduct, PagedResult, Product, ProductQuery, UpdateProduct } from './product.models';

/**
 * Thin, typed wrapper around the products REST endpoints.
 *
 * SINGLE RESPONSIBILITY: this class ONLY knows URLs and HTTP verbs. No state, no UI
 * logic, no error toasts (the interceptors handle those). Components never call
 * HttpClient directly, so if the API changes, only this file changes.
 *
 * HttpClient returns COLD observables: nothing is sent until someone subscribes, and
 * unsubscribing cancels the request (the store's switchMap relies on this).
 */
@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/products`;

  search(query: ProductQuery): Observable<PagedResult<Product>> {
    // Build ?search=..&page=.. while skipping empty values to keep URLs clean
    // (clean, stable URLs also mean better hit rates in the API's output cache).
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDir', query.sortDir);
    if (query.search.trim()) params = params.set('search', query.search.trim());
    if (query.categoryId !== null) params = params.set('categoryId', query.categoryId);
    if (!query.includeDiscontinued) params = params.set('includeDiscontinued', false);

    return this.http.get<PagedResult<Product>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<Product> {
    return this.http.get<Product>(`${this.baseUrl}/${id}`);
  }

  create(product: CreateProduct): Observable<Product> {
    return this.http.post<Product>(this.baseUrl, product);
  }

  update(id: number, product: UpdateProduct): Observable<Product> {
    return this.http.put<Product>(`${this.baseUrl}/${id}`, product);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
