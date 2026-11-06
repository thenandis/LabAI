import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
}

/**
 * App-wide toast notifications, built on SIGNALS.
 *
 * SIGNALS IN ONE PARAGRAPH: a signal is a reactive value. Templates that read
 * `toasts()` are re-rendered automatically when it changes, and ONLY those parts.
 * This app runs ZONELESS (Angular 21's default): there is no zone.js patching every
 * browser API to guess when something changed. Signals tell Angular exactly what
 * changed, which means smaller bundles and faster change detection.
 *
 * `providedIn: 'root'` = a single shared instance (singleton) for the whole app,
 * so any component or interceptor can inject it and raise a toast.
 */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 1;
  private readonly _toasts = signal<Toast[]>([]);

  /** Read-only view: components can read toasts but only this service can change them (encapsulation). */
  readonly toasts = this._toasts.asReadonly();

  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message, 6000);
  }

  info(message: string): void {
    this.show('info', message);
  }

  dismiss(id: number): void {
    // Signals use immutable updates: always produce a NEW array.
    this._toasts.update((list) => list.filter((t) => t.id !== id));
  }

  private show(kind: ToastKind, message: string, durationMs = 4000): void {
    const toast: Toast = { id: this.nextId++, kind, message };
    this._toasts.update((list) => [...list.slice(-3), toast]); // cap at 4 visible
    setTimeout(() => this.dismiss(toast.id), durationMs);
  }
}
