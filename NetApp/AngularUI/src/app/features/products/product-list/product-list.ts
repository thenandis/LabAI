import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import { toProblemDetails } from '../../../core/models/problem-details';
import { NotificationService } from '../../../core/notifications/notification.service';
import { Pagination } from '../../../shared/ui/pagination';
import { LookupApiService } from '../data-access/lookup-api.service';
import { Product, ProductSortField } from '../data-access/product.models';
import { ProductApiService } from '../data-access/product-api.service';
import { ProductStore } from '../data-access/product.store';

/**
 * The "R" and "D" of CRUD: a searchable, sortable, paged product table.
 *
 * SMART component: injects the store and services. It contains no data-fetching logic of
 * its own; it forwards user intent to the store (setSearch, toggleSort...).
 *
 * RESPONSIVE: on wide screens it's a table; below 760px each row is restyled as a card
 * (pure CSS, see styles.scss → .data-table). It's the same DOM, so there's no duplicate template.
 *
 * PERFORMANCE:
 *  - OnPush + signals: re-renders only when a signal it reads changes.
 *  - `@for ... track p.id`: when the page reloads, Angular REUSES the DOM rows for
 *    products it has already rendered rather than rebuilding the whole table.
 *  - Search is debounced, so typing "chai" sends 1 request, not 4.
 */
@Component({
  selector: 'app-product-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, Pagination],
  templateUrl: './product-list.html',
})
export class ProductList {
  protected readonly store = inject(ProductStore);
  private readonly api = inject(ProductApiService);
  private readonly notifications = inject(NotificationService);

  /** toSignal bridges RxJS → signals, so the template just calls categories(). */
  protected readonly categories = toSignal(inject(LookupApiService).categories$, { initialValue: [] });

  protected readonly pageSizes = [10, 25, 50, 100];
  protected readonly searchControl = new FormControl(this.store.currentQuery().search, { nonNullable: true });

  /** The product awaiting delete confirmation (drives the <dialog>). */
  protected readonly pendingDelete = signal<Product | null>(null);
  protected readonly deleting = signal(false);
  private readonly deleteDialog = viewChild.required<ElementRef<HTMLDialogElement>>('deleteDialog');

  constructor() {
    // DEBOUNCED SEARCH (classic RxJS):
    //   debounceTime(300)     → wait until the user pauses typing for 300 ms
    //   map(trim)             → ignore leading/trailing spaces
    //   distinctUntilChanged  → don't re-query if the text didn't actually change
    this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        map((v) => v.trim()),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((term) => this.store.setSearch(term));
  }

  protected onCategoryChange(value: string): void {
    this.store.setCategory(value ? Number(value) : null);
  }

  protected sortIndicator(field: ProductSortField): string {
    const q = this.store.currentQuery();
    if (q.sortBy !== field) return '';
    return q.sortDir === 'asc' ? '▲' : '▼';
  }

  protected ariaSort(field: ProductSortField): 'ascending' | 'descending' | 'none' {
    const q = this.store.currentQuery();
    if (q.sortBy !== field) return 'none';
    return q.sortDir === 'asc' ? 'ascending' : 'descending';
  }

  // ---- Delete flow: confirm in a native <dialog> (accessible, focus-trapped, zero dependencies) ----

  protected askDelete(product: Product): void {
    this.pendingDelete.set(product);
    this.deleteDialog().nativeElement.showModal();
  }

  protected cancelDelete(): void {
    this.deleteDialog().nativeElement.close();
    this.pendingDelete.set(null);
  }

  protected confirmDelete(): void {
    const product = this.pendingDelete();
    if (!product) return;

    this.deleting.set(true);
    this.api.delete(product.id).subscribe({
      next: () => {
        this.notifications.success(`"${product.name}" was deleted.`);
        this.finishDelete();
        this.store.afterDelete();
      },
      error: (err: unknown) => {
        const problem = toProblemDetails(err);
        // 5xx/network errors are already toasted by the interceptor; handle the expected ones here.
        if (problem.status === 404 || problem.status === 409) {
          this.notifications.error(problem.detail ?? 'The product could not be deleted.');
          this.store.reload();
        }
        this.finishDelete();
      },
    });
  }

  private finishDelete(): void {
    this.deleting.set(false);
    this.cancelDelete();
  }
}
