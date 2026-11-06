import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

/**
 * Smoke test for the app shell. Angular 21 runs unit tests with Vitest (`ng test`):
 * fast, ESM-native, and Jest-compatible syntax (describe / it / expect).
 */
describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('renders the brand and main navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('.brand__name')?.textContent).toContain('Northwind Catalog');
    expect(el.querySelector('nav[aria-label="Main"] a')?.textContent).toContain('Products');
  });
});
