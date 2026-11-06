# Interview Guide: walking through this codebase

Use this when presenting the project or answering design questions. Every claim
points to a file you can open and show.

---

## 1. The 2-minute walkthrough

> "It's a three-tier CRUD app over Microsoft's Northwind data: Angular 21, .NET 10 and SQL Server.
>
> The **API uses Clean Architecture** in four projects. **Domain** holds the `Product` entity with its
> business rules and has no dependencies. **Application** holds the use cases: `ProductService`,
> FluentValidation validators, and a `Result<T>` type instead of exceptions for expected failures. It
> also **defines** the interfaces it needs: `IProductRepository`, `IProductQueries` and `IUnitOfWork`.
> **Infrastructure** implements them with EF Core. The **Api** project is a thin Minimal API layer that maps
> `Result` to HTTP, and Program.cs is the composition root.
>
> I split reads from writes (**CQRS-lite**): writes load tracked entities so domain rules run, and reads
> project straight to DTOs in SQL with `AsNoTracking`. **Optimistic concurrency** uses SQL Server
> `ROWVERSION`, so two users editing the same product get a 409 rather than a silent lost update.
>
> For production: pooled DbContext, output caching with tag eviction, compression, rate limiting,
> ProblemDetails errors with trace IDs, health checks, request timeouts, chiseled non-root containers
> and a least-privilege DB login.
>
> The **Angular** side is standalone and zoneless with signals. A small signal store uses `switchMap`
> to avoid race conditions, functional interceptors handle loading, errors and retries, and the form maps
> server validation errors onto fields and handles the 409 with a 'reload latest' banner.
>
> Testing: 27 unit tests with NSubstitute fakes, 9 integration tests through the real HTTP pipeline with
> `WebApplicationFactory` and in-memory SQLite, and Vitest specs on the UI."

---

## 2. Concept → file map

### SOLID
| Principle | Where to point |
|---|---|
| **S**ingle Responsibility | `ProductService` orchestrates only; rules live in `Product.cs`, validation in `ProductValidators.cs`, SQL in `ProductQueries.cs`, HTTP in `ProductEndpoints.cs`. |
| **O**pen/Closed | `AuditableEntityInterceptor`: new auditable entities get timestamps by implementing `IAuditable`, with no interceptor change. `IProductService` can be wrapped in a decorator. |
| **L**iskov Substitution | Any `IProductRepository` (EF, in-memory fake) works. `ProductServiceTests` substitutes fakes. |
| **I**nterface Segregation | `IProductRepository` (write) vs `IProductQueries` (read) vs `ILookupQueries`: small, focused interfaces. |
| **D**ependency Inversion | Application **owns** the interfaces and Infrastructure implements them (`Application/Abstractions/*`). |

### Architecture & patterns
| Concept | File |
|---|---|
| Clean / Onion architecture | the 4 projects under `CSharpAPI/src` (see each `.csproj` comment) |
| Composition root | `SharpAPI.Api/Program.cs` |
| Rich domain model, factory method, invariants | `Domain/Entities/Product.cs` |
| Parameter object | `Domain/Entities/ProductDetails.cs` |
| Result pattern | `Application/Common/Result.cs`, `Error.cs` |
| Repository + Unit of Work | `IProductRepository`, `IUnitOfWork`, `Infrastructure/Persistence/UnitOfWork.cs` |
| CQRS-lite | `IProductRepository` vs `IProductQueries` |
| Adapter (Ports & Adapters) | `UnitOfWork` translates `DbUpdateConcurrencyException` → `ConcurrencyConflictException` |
| Options pattern + fail-fast validation | `DatabaseOptions.cs`, `AddOptions().Validate().ValidateOnStart()` |
| Interceptor (cross-cutting) | `AuditableEntityInterceptor.cs`, `SqliteRowVersionInterceptor.cs` |
| DTO / anti-over-posting | `ProductDto.cs`, `ProductRequests.cs` |
| Expression-tree projection | `ProductMappings.ToDto` |

### Data & EF Core
| Concept | File |
|---|---|
| Optimistic concurrency (rowversion) | `02_Schema.sql`, `ProductConfiguration.cs`, `ProductRepository.SetExpectedRowVersion` |
| Fluent API mapping, database-first | `Persistence/Configurations/*` |
| DbContext pooling, retry-on-failure | `Infrastructure/DependencyInjection.cs` |
| AsNoTracking, projection, paging, stable sort | `ProductQueries.cs` |
| Safe dynamic sorting (whitelist) | `ProductSortFields` + the `Sorts` dictionary |
| LIKE wildcard escaping | `ProductQueries.EscapeLike` |
| Indexing strategy, RCSI, CHECK constraints | `SQLServerDb/scripts/01_*.sql`, `02_*.sql` |
| Least privilege | `04_Security.sql` |

### ASP.NET Core
| Concept | File |
|---|---|
| Minimal APIs, route groups, TypedResults | `Endpoints/ProductEndpoints.cs` |
| Middleware ordering | `Extensions/WebApplicationExtensions.cs` |
| IExceptionHandler + ProblemDetails | `ErrorHandling/GlobalExceptionHandler.cs`, `ResultExtensions.cs` |
| Output caching + tag eviction | `ServiceCollectionExtensions.cs`, `CachePolicies.cs`, endpoint handlers |
| Rate limiting, request timeouts, compression, CORS | `ServiceCollectionExtensions.cs` |
| Custom middleware | `Middleware/SecurityHeadersMiddleware.cs` |
| STJ source generation | `Serialization/AppJsonSerializerContext.cs` |
| Health checks (liveness vs readiness) | `WebApplicationExtensions.MapApiEndpoints` |
| DI lifetimes | comments in `Application/DependencyInjection.cs` |

### Angular
| Concept | File |
|---|---|
| Standalone + zoneless bootstrap | `app.config.ts`, `angular.json` (no zone.js polyfill) |
| Signals, computed, signal store | `features/products/data-access/product.store.ts` |
| RxJS: switchMap, debounceTime, shareReplay | `product.store.ts`, `product-list.ts`, `lookup-api.service.ts` |
| Functional interceptors | `core/http/http.interceptors.ts` |
| InjectionToken config | `core/api.config.ts` |
| Lazy loading, route providers, functional guards | `app.routes.ts`, `products.routes.ts` |
| Route params as inputs | `withComponentInputBinding()` + `ProductForm.id` |
| Typed reactive forms, server-error mapping | `product-form/product-form.ts` |
| Smart vs presentational components, `input()`/`output()` | `product-list.ts` vs `shared/ui/pagination.ts` |
| New control flow (`@if`/`@for`/`track`) | `product-list.html` |
| Accessibility | `aria-sort`, `aria-live`, native `<dialog>`, skip link, `:focus-visible` |
| Responsive (table → cards), dark mode | `styles.scss` |

---

## 3. Likely questions, with answers grounded in this code

**Q: Why Clean Architecture? Isn't it overkill for CRUD?**
For 5 endpoints it's more ceremony than strictly needed. I chose it because it shows how the app *scales*: business rules are testable without a DB, EF Core could be swapped for Dapper by touching one project, and the boundaries stop the "fat controller" problem. I kept it pragmatic: there's no MediatR, no AutoMapper, and lookups skip a pointless service layer (YAGNI).

**Q: Why the Result pattern rather than exceptions?**
"Not found" and "invalid input" are *expected* outcomes. Exceptions are slow (stack capture), invisible in signatures and easy to forget to catch. `Result<T>` makes failure explicit. Exceptions remain for truly exceptional cases (DB down), handled once by `GlobalExceptionHandler`.

**Q: Repository over EF Core? EF already is a repository and unit of work.**
True, and that's why `UnitOfWork` is a 10-line wrapper. The value is **dependency inversion**: the Application layer has zero EF references, and the services are unit-tested with fakes. I avoided the anti-pattern of a generic `IRepository<T>` that leaks `IQueryable`. The read side uses purpose-built query objects instead.

**Q: How do you prevent lost updates?**
`ROWVERSION` column → EF concurrency token. The client gets `rowVersion` with the product and sends it back on PUT. `SetExpectedRowVersion` makes EF issue `UPDATE ... WHERE Id=@id AND RowVersion=@clientVersion`. Zero rows affected → `DbUpdateConcurrencyException` → `ConcurrencyConflictException` → 409. The UI shows "reload latest". This is covered by `Update_WithStaleRowVersion_Returns409Conflict`.

**Q: How did you make it fast?**
- DB: indexes on FKs and the search/sort columns; RCSI so reads don't block writes.
- EF: pooled contexts, `AsNoTracking`, projection (only the needed columns, joins in SQL), server-side paging.
- HTTP: output cache (repeat GETs never hit the DB), evicted by tag on writes; Brotli; STJ source-gen.
- UI: zoneless + signals + OnPush, lazy chunks (~82 KB initial), `track` in `@for`, debounced search, client-side `shareReplay` for lookups, `switchMap` cancelling stale requests.

**Q: Output caching with multiple instances?**
The default store is in-memory per instance, so instance B wouldn't see A's eviction. Use the Redis output-cache store (`AddStackExchangeRedisOutputCache`), or a short TTL if slight staleness is acceptable.

**Q: Offset vs keyset pagination?**
Offset (`Skip/Take`) is simple and supports "jump to page N", which suits an admin grid. It degrades on deep pages because the DB still walks the skipped rows. For feeds or infinite scroll, use keyset: `WHERE (Name, Id) > (@lastName, @lastId)`. Either way, a deterministic tie-breaker (`ThenBy(Id)`) is essential. This code has one.

**Q: What does `[AsParameters]` do?**
It binds a record's properties from the query string. The constructor defaults make every parameter optional.

**Q: Explain the middleware order.**
The exception handler is outermost so it catches everything. Status-code pages turn empty 4xx into ProblemDetails. Then security headers, then compression (before anything writes the body), CORS (before caching so preflights work), the rate limiter (reject early and cheaply), request timeouts, and finally the output cache (short-circuits on a hit).

**Q: DI lifetimes, and what's a captive dependency?**
Transient = per resolve, scoped = per request, singleton = per app. A captive dependency is a singleton holding a scoped service (e.g. a DbContext), which ends up shared across requests and threads. Here the interceptors are singletons and stateless (required by pooling), and everything that touches the DbContext is scoped.

**Q: Liveness vs readiness?**
Liveness answers "is the process alive?" Failure → restart. It runs no checks here. Readiness answers "can it serve?" Failure → removed from the load balancer, but not restarted. It checks the DB. Putting the DB check in liveness would cause restart storms whenever the DB blips.

**Q: How is the API secured?**
Not authenticated yet; that's the first next step (JWT bearer + `RequireAuthorization()`). Already in place: rate limiting, body-size limit, security headers + CSP, no stack traces in prod, parameterised SQL, whitelisted sort keys, a least-privilege DB user (no DDL), non-root chiseled containers, and NuGet vulnerability auditing with transitive pinning (the build caught a vulnerable `System.Security.Cryptography.Pkcs` from SqlClient, now pinned to the patched version).

**Q: Why signals and zoneless in Angular?**
zone.js monkey-patches every async API and re-checks the whole tree after any event. Signals tell Angular exactly what changed, so updates are fine-grained, bundles are smaller (no zone.js) and change detection is predictable. Zoneless is the default in Angular 21.

**Q: Why `switchMap` in the store?**
It cancels the previous HTTP request when a new query arrives. Without it, a slow response for page 1 could arrive after page 2's and overwrite it. `mergeMap` would run both, `concatMap` would queue them, and `exhaustMap` would ignore new ones (that one suits "submit" buttons).

**Q: How do server validation errors reach the form?**
The API returns `400` with `errors: { "unitPrice": ["..."] }` (keys camelCased in `ResultExtensions`). `ProductForm.applyServerErrors` calls `control.setErrors({ server: msg })` for each key, so the message appears under the right field, including server-only rules such as "Category 99 does not exist".

**Q: How do you test this?**
- **Unit** (`SharpAPI.UnitTests`): domain invariants with `[Theory]`, validators, and service orchestration with NSubstitute. They take milliseconds.
- **Integration** (`SharpAPI.IntegrationTests`): `WebApplicationFactory` boots the real pipeline over in-memory SQLite and exercises full CRUD, 400/404/409, cache eviction, security headers and health. For more fidelity, use Testcontainers with real SQL Server.
- **UI** (Vitest): presentational component tests, `HttpTestingController` for exact URL/verb/body assertions.

**Q: What would you improve with more time?**
Auth (JWT + roles), OpenTelemetry tracing, Redis output cache, Testcontainers, API versioning via `Asp.Versioning`, a DACPAC/SSDT project for the DB, query-string-synced list state (shareable URLs), E2E tests with Playwright, and CI/CD with container scanning.

---

## 4. Live-demo checklist

1. `dotnet run` → open `/scalar` and show the OpenAPI docs; run a GET.
2. `npm start` → show search (watch a single request in DevTools after typing), sort, paging, and mobile view (DevTools device toolbar).
3. Create a product with an invalid price → show the field errors (client side), then bypass with curl → server 400.
4. **Concurrency demo:** open the same product in two tabs, save in tab 1, then save in tab 2 → the 409 banner appears → "Reload latest".
5. Show the EF SQL in the API console (Development logs the generated SQL): note the projection, `OFFSET/FETCH`, and the `WHERE RowVersion = @p` on update.
6. `dotnet test` → 36 green tests.
