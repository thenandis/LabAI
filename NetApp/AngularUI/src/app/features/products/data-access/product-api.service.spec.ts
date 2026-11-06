import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '../../../core/api.config';
import { DEFAULT_PRODUCT_QUERY } from './product.models';
import { ProductApiService } from './product-api.service';

/**
 * HttpTestingController intercepts HttpClient calls: no real network. We assert on
 * the exact URL, verb and body the service sends, then "flush" a fake response.
 */
describe('ProductApiService', () => {
  let api: ProductApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: '/test-api' }, // DI makes the base URL swappable
      ],
    });
    api = TestBed.inject(ProductApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify()); // fails if any request was unexpected or never made

  it('builds a clean query string, omitting empty filters', () => {
    api.search({ ...DEFAULT_PRODUCT_QUERY, search: '  chai ', page: 2 }).subscribe();

    const req = http.expectOne((r) => r.url === '/test-api/products');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('search')).toBe('chai');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.has('categoryId')).toBe(false);
    expect(req.request.params.has('includeDiscontinued')).toBe(false);
    req.flush({ items: [], page: 2, pageSize: 10, totalCount: 0, totalPages: 0, hasPreviousPage: true, hasNextPage: false });
  });

  it('sends the rowVersion with PUT for optimistic concurrency', () => {
    const body = {
      name: 'Chai', categoryId: 1, supplierId: 1, quantityPerUnit: null, unitPrice: 18,
      unitsInStock: 39, unitsOnOrder: 0, reorderLevel: 10, discontinued: false, rowVersion: 'AAAAAAAAB9E=',
    };
    api.update(1, body).subscribe();

    const req = http.expectOne('/test-api/products/1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.rowVersion).toBe('AAAAAAAAB9E=');
    req.flush({ ...body, id: 1 });
  });

  it('issues DELETE to the product URL', () => {
    api.delete(5).subscribe();

    const req = http.expectOne('/test-api/products/5');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });
});
