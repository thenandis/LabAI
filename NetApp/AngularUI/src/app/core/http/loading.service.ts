import { Injectable, computed, signal } from '@angular/core';

/**
 * Counts in-flight HTTP requests, so the shell can show a global progress bar.
 *
 * A COUNTER (not a boolean) because requests overlap: with two parallel calls, the
 * first to finish must not hide the bar while the second is still running.
 */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly pending = signal(0);

  /** computed() = a derived signal; it recalculates only when `pending` changes. */
  readonly isLoading = computed(() => this.pending() > 0);

  start(): void {
    this.pending.update((n) => n + 1);
  }

  stop(): void {
    this.pending.update((n) => Math.max(0, n - 1));
  }
}
