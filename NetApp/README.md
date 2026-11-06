# Northwind Catalog: Angular + ASP.NET Core + SQL Server

A production-style **CRUD** reference app built on Microsoft's **Northwind** sample data.

| Layer | Folder | Tech |
|---|---|---|
| UI | [`AngularUI/`](AngularUI/) | Angular 21: standalone, **zoneless**, **signals**, typed reactive forms, lazy routes, Vitest |
| API | [`CSharpAPI/`](CSharpAPI/) | .NET 10 **Minimal API**, **Clean Architecture**, EF Core 10, FluentValidation, xUnit v3 |
| DB | [`SQLServerDb/`](SQLServerDb/) | SQL Server 2022: idempotent T-SQL scripts, rowversion concurrency, least-privilege login |

> **Interview prep?** Read [INTERVIEW-GUIDE.md](INTERVIEW-GUIDE.md): a map from every concept to the file that demonstrates it, plus likely questions and answers.

---

## Architecture

```
┌──────────── Browser ────────────┐
│ Angular 21 SPA                  │
│  ProductList / ProductForm      │  signals + OnPush, debounced search, switchMap
│        │                        │
│  ProductStore (signal store)    │
│  ProductApiService (HttpClient) │
│  Interceptors: loading → error → retry
└────────┬────────────────────────┘
         │  HTTPS  /api/v1/products   (same origin via nginx; dev: ng proxy)
┌────────▼────────────────────────────────────────────────────────────────┐
│ ASP.NET Core (.NET 10)                                                  │
│  Middleware: ExceptionHandler → SecurityHeaders → Compression → CORS    │
│              → RateLimiter → RequestTimeouts → OutputCache              │
│                                                                         │
│  [Api]            ProductEndpoints  (thin: bind → call → map Result)    │
│     │                                                                   │
│  [Application]    IProductService ─ validators ─ Result<T> ─ DTOs       │
│     │  owns interfaces: IProductRepository, IProductQueries, IUnitOfWork│
│  [Domain]         Product entity (invariants, NeedsReorder rule)        │
│     ▲                                                                   │
│  [Infrastructure] EF Core: AppDbContext (pooled), repositories,         │
│                   query objects, interceptors, health checks            │
└────────┬────────────────────────────────────────────────────────────────┘
         │  TDS, parameterised SQL, retry-on-transient-failure
┌────────▼─────────────────────┐
│ SQL Server: NorthwindCatalog │  RCSI on, FK indexes, CHECK constraints, ROWVERSION
└──────────────────────────────┘
```

**Dependency rule:** `Api → Application → Domain ← Infrastructure`. The Domain has no dependencies at all, and the Application layer never references EF Core or ASP.NET.

---

## Quick start

### Option A: zero install (SQLite), the fastest way to see it run

Needs the .NET 10 SDK and Node 22+. In development the API uses a **SQLite** file seeded with the same 77 Northwind products, so no SQL Server or Docker is needed.

```bash
# terminal 1: API on http://localhost:5080  (interactive docs at /scalar)
cd CSharpAPI/src/SharpAPI.Api
dotnet run

# terminal 2: UI on http://localhost:4200
cd AngularUI
npm install            # with Node 23, run `npx npm@11 install` (npm 10.9.0 has a resolver bug)
npm start
```

### Option B: full stack in Docker with real SQL Server

```bash
cp .env.example .env        # optional: set your own passwords
docker compose up --build
```

| URL | What |
|---|---|
| http://localhost:8080 | Angular UI (nginx; proxies `/api`) |
| http://localhost:5080/health/ready | API readiness (checks the DB) |
| `localhost,1433` | SQL Server (`sa` / `SA_PASSWORD`) for SSMS / Azure Data Studio |

### Option C: an existing SQL Server

```bash
cd SQLServerDb
SQL_SERVER="myserver,1433" SA_PASSWORD='...' APP_PASSWORD='...' ./deploy.sh

cd ../CSharpAPI/src/SharpAPI.Api
dotnet run --launch-profile "http (SQL Server in Docker)"
# or set the env vars: Database__Provider=SqlServer  ConnectionStrings__Catalog="Server=...;..."
```

---

## API

Base path `/api/v1`. All errors are **RFC 9457 ProblemDetails** (`application/problem+json`) with a `traceId` and a machine-readable `code`.

| Method | Route | Success | Errors |
|---|---|---|---|
| GET | `/products?search=&categoryId=&supplierId=&includeDiscontinued=&sortBy=&sortDir=&page=&pageSize=` | 200 `PagedResult<ProductDto>` | 400 |
| GET | `/products/{id}` | 200 `ProductDto` | 404 |
| POST | `/products` | 201 + `Location` | 400 |
| PUT | `/products/{id}` (body includes `rowVersion`) | 200 `ProductDto` (new `rowVersion`) | 400, 404, **409** |
| DELETE | `/products/{id}` | 204 | 404, 409 |
| GET | `/lookups/categories`, `/lookups/suppliers` | 200 `LookupDto[]` | |
| GET | `/health/live`, `/health/ready` | 200 | 503 |

`sortBy` ∈ `id | name | category | unitPrice | unitsInStock`, `sortDir` ∈ `asc | desc`, `pageSize` ≤ 100.

```bash
curl "http://localhost:5080/api/v1/products?search=ch&sortBy=unitPrice&sortDir=desc&pageSize=5"
```

---

## Tests

```bash
cd CSharpAPI && dotnet test        # 27 unit + 9 integration tests (WebApplicationFactory + in-memory SQLite)
cd AngularUI && npm run test:ci    # Vitest: component, service (HttpTestingController), shell
```

---

## What makes it "production-ready"

| Concern | How | Where |
|---|---|---|
| Performance | DbContext **pooling**, `AsNoTracking` + SQL **projection**, server-side paging, **output cache** with tag eviction, Brotli/gzip, STJ **source generation**, Server GC | `Infrastructure/DependencyInjection.cs`, `ProductQueries.cs`, `ServiceCollectionExtensions.cs` |
| Correctness | Optimistic concurrency (**ROWVERSION** → 409), validation in 3 layers (UI, FluentValidation, domain guards + SQL CHECKs) | `ProductService.cs`, `ProductRepository.cs`, `Product.cs`, `02_Schema.sql` |
| Resilience | EF **retry on transient failure**, request timeouts, client GET retry with back-off, health checks (live/ready) | `DependencyInjection.cs`, `http.interceptors.ts` |
| Security | Rate limiting, security headers + CSP, 1 MB body limit, no stack traces in prod, whitelisted sorting, parameterised SQL, **least-privilege** DB login, non-root **chiseled** containers, NuGet audit + transitive pinning | `SecurityHeadersMiddleware.cs`, `04_Security.sql`, `Dockerfile`s, `Directory.Packages.props` |
| Observability | Structured JSON logs, `traceId` in every error, EF SQL logging in dev | `appsettings*.json`, `GlobalExceptionHandler.cs` |
| Front-end UX | Zoneless + signals + OnPush, lazy chunks (~82 KB initial transfer), debounced search, skeletons, a11y (aria-sort, live regions, native `<dialog>`), dark mode, mobile card layout | `AngularUI/src/...` |
| Delivery | Multi-stage Docker builds, compose with health-gated startup, idempotent DB scripts, Central Package Management, warnings-as-errors | `docker-compose.yml`, `Directory.*.props` |

### Next steps for a real production rollout
- **AuthN/AuthZ**: JWT bearer (Entra ID / Auth0) + `RequireAuthorization()` on write endpoints; an Angular auth interceptor.
- **Distributed cache**: Redis output-cache store, so eviction reaches every instance.
- **OpenTelemetry**: traces/metrics to Azure Monitor, Grafana or Jaeger.
- **CI/CD**: build → test → container scan → deploy; DB scripts via a DACPAC/SSDT project or migrations bundle.
- **Testcontainers**: run the integration tests against real SQL Server.
