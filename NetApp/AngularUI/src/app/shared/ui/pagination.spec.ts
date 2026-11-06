import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Pagination } from './pagination';

/**
 * Presentational components are easy to test: set inputs, click, assert on outputs.
 * There are no services to mock, which is a big reason to keep "dumb" components dumb.
 */
describe('Pagination', () => {
  let fixture: ComponentFixture<Pagination>;

  async function render(page: number, totalPages: number) {
    fixture = TestBed.createComponent(Pagination);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('totalPages', totalPages);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders nothing when there is a single page', async () => {
    const el = await render(1, 1);
    expect(el.querySelector('nav')).toBeNull();
  });

  it('collapses long ranges with ellipses around the current page', async () => {
    const el = await render(6, 20);
    const labels = Array.from(el.querySelectorAll('.pagination__page, .pagination__gap')).map((n) => n.textContent?.trim());
    expect(labels).toEqual(['1', '…', '5', '6', '7', '…', '20']);
  });

  it('marks the current page for assistive technology', async () => {
    const el = await render(3, 5);
    expect(el.querySelector('[aria-current="page"]')?.textContent?.trim()).toBe('3');
  });

  it('emits pageChange when Next is clicked, and disables Prev on page 1', async () => {
    const el = await render(1, 5);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((p) => emitted.push(p));

    const [prev, next] = [el.querySelector<HTMLButtonElement>('[aria-label="Previous page"]')!, el.querySelector<HTMLButtonElement>('[aria-label="Next page"]')!];
    expect(prev.disabled).toBe(true);
    next.click();

    expect(emitted).toEqual([2]);
  });
});
