import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { API_BASE_URL } from '../../../core/api.config';
import { Lookup } from './product.models';

/**
 * Dropdown reference data (categories, suppliers), cached on the CLIENT.
 *
 * `shareReplay(1)` turns the HTTP call into a cached, shared stream: the FIRST
 * subscriber triggers the request, and every later subscriber (opening the form again,
 * the list's category filter...) gets the cached value instantly with no new request.
 * Combined with the API's 5-minute output cache, lookups are almost free.
 */
@Injectable({ providedIn: 'root' })
export class LookupApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/lookups`;

  readonly categories$: Observable<Lookup[]> = this.http
    .get<Lookup[]>(`${this.baseUrl}/categories`)
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));

  readonly suppliers$: Observable<Lookup[]> = this.http
    .get<Lookup[]>(`${this.baseUrl}/suppliers`)
    .pipe(shareReplay({ bufferSize: 1, refCount: false }));
}
