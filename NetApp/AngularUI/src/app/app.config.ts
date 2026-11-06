import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { PreloadAllModules, provideRouter, withComponentInputBinding, withPreloading, withViewTransitions } from '@angular/router';
import { errorInterceptor, loadingInterceptor, retryInterceptor } from './core/http/http.interceptors';
import { routes } from './app.routes';

/**
 * Application-wide providers: the Angular counterpart of Program.cs in the API.
 *
 * STANDALONE APIs: there's no AppModule / NgModules. Everything is configured with
 * provide*() functions, which are tree-shakable, so unused features add zero bytes.
 *
 * ZONELESS: Angular 21 apps are zoneless by default (no zone.js in the bundle; see
 * angular.json, which has no polyfills). Change detection is driven by signals and
 * events, so it's smaller and faster, with no "magic" re-renders.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    provideRouter(
      routes,
      withComponentInputBinding(),           // route params → component input() (e.g. ProductForm.id)
      withViewTransitions(),                 // smooth native page transitions (View Transitions API)
      withPreloading(PreloadAllModules),     // after first paint, prefetch lazy chunks in the background
    ),

    provideHttpClient(
      withFetch(), // uses the modern fetch() API under the hood
      // Order matters, like ASP.NET middleware: loading wraps everything, retry sits closest
      // to the network (so the error toast fires only once, after retries are exhausted).
      withInterceptors([loadingInterceptor, errorInterceptor, retryInterceptor]),
    ),
  ],
};
