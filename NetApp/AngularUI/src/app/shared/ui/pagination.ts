import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/**
 * Reusable, "dumb" (presentational) pagination control.
 *
 * SMART vs DUMB components:
 *  - Smart (container) components, like ProductList, inject services and own the state.
 *  - Dumb (presentational) components, like this one, only receive inputs and emit
 *    outputs. They know nothing about products or HTTP, so they're reusable anywhere
 *    and trivial to test.
 *
 * `input()` / `output()` are the signal-based replacements for @Input() / @Output().
 * `input.required` makes a missing binding a COMPILE error.
 */
@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalPages() > 1) {
      <nav class="pagination" aria-label="Pagination">
        <button type="button" class="btn btn--ghost" [disabled]="page() <= 1" (click)="go(page() - 1)" aria-label="Previous page">
          ‹ Prev
        </button>

        @for (p of pages(); track $index) {
          @if (p === null) {
            <span class="pagination__gap" aria-hidden="true">…</span>
          } @else {
            <button
              type="button"
              class="btn btn--ghost pagination__page"
              [class.is-active]="p === page()"
              [attr.aria-current]="p === page() ? 'page' : null"
              (click)="go(p)"
            >
              {{ p }}
            </button>
          }
        }

        <button type="button" class="btn btn--ghost" [disabled]="page() >= totalPages()" (click)="go(page() + 1)" aria-label="Next page">
          Next ›
        </button>
      </nav>
    }
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();

  /**
   * A compact page list: always the first and last pages, plus a window around the
   * current one, with null standing for "…".
   * e.g. page 6 of 20 → [1, null, 5, 6, 7, null, 20]
   */
  protected readonly pages = computed<(number | null)[]>(() => {
    const current = this.page();
    const total = this.totalPages();
    const result: (number | null)[] = [];
    for (let p = 1; p <= total; p++) {
      if (p === 1 || p === total || Math.abs(p - current) <= 1) {
        result.push(p);
      } else if (result[result.length - 1] !== null) {
        result.push(null);
      }
    }
    return result;
  });

  protected go(page: number): void {
    if (page >= 1 && page <= this.totalPages() && page !== this.page()) {
      this.pageChange.emit(page);
    }
  }
}
