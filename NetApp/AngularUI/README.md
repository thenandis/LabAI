# AngularUI: Northwind Catalog front end

Angular 21 (standalone, zoneless, signals) CRUD UI for the SharpAPI products API.
See the main [README](../README.md) for the architecture and how to run the full stack.

```bash
npm install          # first time (with Node 23, use: npx npm@11 install)
npm start            # http://localhost:4200, proxies /api → http://localhost:5080
npm run test:ci      # Vitest unit tests
npm run build        # production build → dist/angular-ui/browser
```

## Folder map

```
src/app/
├── app.config.ts            # providers: router, HttpClient + interceptors (like Program.cs)
├── app.routes.ts            # top-level lazy routes
├── core/                    # app-wide singletons
│   ├── api.config.ts        # API_BASE_URL InjectionToken
│   ├── http/                # loading / error / retry interceptors
│   ├── models/              # ProblemDetails (RFC 9457) helpers
│   └── notifications/       # toast service (signals) + container
├── shared/ui/               # reusable presentational components (pagination)
└── features/products/       # the CRUD feature (lazy-loaded)
    ├── data-access/         # models, API services, signal store
    ├── product-list/        # search / sort / page / delete
    ├── product-form/        # create + edit, server validation, 409 handling
    └── products.routes.ts   # feature routes + route-scoped store + guards
```
