import { HttpErrorResponse } from '@angular/common/http';

/**
 * RFC 9457 "Problem Details": the JSON shape the C# API returns for EVERY error
 * (see ResultExtensions.ToProblem and GlobalExceptionHandler on the server).
 * One consistent shape means the UI can handle errors generically.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  /** Stable machine-readable code, e.g. "Product.ConcurrencyConflict". */
  code?: string;
  /** Validation failures: field name (camelCase) → messages. Present on 400s. */
  errors?: Record<string, string[]>;
}

/** Safely pulls a ProblemDetails out of an HttpErrorResponse (the body may be empty or not JSON). */
export function toProblemDetails(error: unknown): ProblemDetails {
  if (error instanceof HttpErrorResponse) {
    const body = error.error;
    if (body && typeof body === 'object') {
      return { status: error.status, ...(body as ProblemDetails) };
    }
    return {
      status: error.status,
      title: error.status === 0 ? 'Cannot reach the server' : error.statusText,
    };
  }
  return { title: 'Unexpected error' };
}
