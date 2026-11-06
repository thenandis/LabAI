import { InjectionToken } from '@angular/core';

/**
 * Base URL of the REST API, provided through Angular's dependency injection.
 *
 * WHY AN InjectionToken rather than a hard-coded string or `environment.ts`?
 *  - Services ask for the value (`inject(API_BASE_URL)`) instead of importing a constant,
 *    the same Dependency Inversion idea as in the C# API.
 *  - Tests can provide a different value.
 *  - The default is RELATIVE ('/api/v1'), so the SAME build works everywhere:
 *      • local dev: the Angular dev server proxies /api → http://localhost:5080 (proxy.conf.json)
 *      • production: nginx serves the UI and proxies /api → the API container (nginx.conf)
 *    Same origin means no CORS, and you build once and deploy to any environment.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api/v1',
});
