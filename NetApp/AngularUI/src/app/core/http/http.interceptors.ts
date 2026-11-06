import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize, retry, tap, throwError, timer } from 'rxjs';
import { toProblemDetails } from '../models/problem-details';
import { NotificationService } from '../notifications/notification.service';
import { LoadingService } from './loading.service';

/**
 * HTTP INTERCEPTORS: middleware for HttpClient, the Angular counterpart of
 * ASP.NET Core middleware. Every request passes through them in the order they are
 * registered in app.config.ts. These are FUNCTIONAL interceptors (plain functions
 * using inject()), the modern style that replaced class-based interceptors.
 *
 * Cross-cutting concerns live here ONCE, not copy-pasted into every service call.
 */

/** Drives the global progress bar for every request. */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loading = inject(LoadingService);
  loading.start();
  // finalize runs on success, error AND cancellation, so the counter never leaks.
  return next(req).pipe(finalize(() => loading.stop()));
};

/**
 * Resilience: retries IDEMPOTENT requests (GET) on transient failures.
 *
 * Only GETs are retried, because retrying a POST could create a duplicate product.
 * Only transient failures (network drop, 502/503/504) are retried; a 400 or 404 will
 * never succeed on retry. The delay grows (exponential back-off: 300 ms, then 600 ms),
 * so a struggling server isn't hammered.
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET') return next(req);

  return next(req).pipe(
    retry({
      count: 2,
      delay: (error: unknown, attempt: number) => {
        const transient =
          error instanceof HttpErrorResponse && [0, 502, 503, 504].includes(error.status);
        return transient ? timer(300 * 2 ** (attempt - 1)) : throwError(() => error);
      },
    }),
  );
};

/**
 * Centralised error UX. Errors the USER can't fix (network down, 5xx, rate limit)
 * become a toast here. Errors the CALLING COMPONENT should handle (400 validation,
 * 404, 409 conflict) pass through untouched, so the form can show them in context.
 * Either way the error is re-thrown, so callers still see it.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    // tap({ error }) OBSERVES the error without swallowing it: the error still reaches the caller.
    tap({
      error: (err: unknown) => {
        if (!(err instanceof HttpErrorResponse)) return;
        const problem = toProblemDetails(err);

        if (err.status === 0) {
          notifications.error('Cannot reach the server. Check your connection and try again.');
        } else if (err.status === 429) {
          notifications.error('Too many requests. Please slow down and retry in a few seconds.');
        } else if (err.status >= 500) {
          // traceId lets support find the exact server log entry for this failure.
          const trace = problem.traceId ? ` (trace: ${problem.traceId})` : '';
          notifications.error(`${problem.title ?? 'Server error'}${trace}`);
        }
      },
    }),
  );
};
