/**
 * TypeScript mirrors of the C# DTOs (SharpAPI.Application/Products/*.cs).
 * Keeping them in sync gives end-to-end type safety: a typo in a field name is a
 * compile error, not a runtime "undefined".
 * (At scale, generate these from the API's OpenAPI document, /openapi/v1.json.)
 */

/** = ProductDto (what the API returns). */
export interface Product {
  id: number;
  name: string;
  supplierId: number | null;
  supplierName: string | null;
  categoryId: number | null;
  categoryName: string | null;
  quantityPerUnit: string | null;
  unitPrice: number;
  unitsInStock: number;
  unitsOnOrder: number;
  reorderLevel: number;
  discontinued: boolean;
  needsReorder: boolean;
  /** Base64 concurrency token. Send it back unchanged on update. */
  rowVersion: string;
}

/** = CreateProductRequest. Utility types derive it from Product, so the two can't drift. */
export type CreateProduct = Omit<Product, 'id' | 'supplierName' | 'categoryName' | 'needsReorder' | 'rowVersion'>;

/** = UpdateProductRequest */
export type UpdateProduct = CreateProduct & Pick<Product, 'rowVersion'>;

/** = PagedResult<T> */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** = LookupDto (dropdown options) */
export interface Lookup {
  id: number;
  name: string;
}

/** Must match ProductSortFields on the server (it rejects anything else with a 400). */
export type ProductSortField = 'id' | 'name' | 'category' | 'unitPrice' | 'unitsInStock';
export type SortDirection = 'asc' | 'desc';

/** = ProductSearchQuery (sent as query-string parameters) */
export interface ProductQuery {
  search: string;
  categoryId: number | null;
  includeDiscontinued: boolean;
  sortBy: ProductSortField;
  sortDir: SortDirection;
  page: number;
  pageSize: number;
}

export const DEFAULT_PRODUCT_QUERY: ProductQuery = {
  search: '',
  categoryId: null,
  includeDiscontinued: true,
  sortBy: 'name',
  sortDir: 'asc',
  page: 1,
  pageSize: 10,
};
