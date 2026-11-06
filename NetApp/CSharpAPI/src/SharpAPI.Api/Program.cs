// =============================================================================
//  Program.cs: the COMPOSITION ROOT and HTTP pipeline of the API.
//
//  Request flow through the layers (Clean Architecture):
//
//    Angular ──HTTP──▶ [Api] Minimal API endpoint (ProductEndpoints)
//                         │  binds JSON / query → request DTO
//                         ▼
//                      [Application] IProductService → validators, Result<T>
//                         │  uses interfaces it OWNS (IProductRepository, IUnitOfWork...)
//                         ▼
//                      [Domain] Product entity enforces business rules
//                         ▲
//                      [Infrastructure] EF Core implements those interfaces → SQL Server
//
//  Dependencies point INWARD: Api → Application → Domain ← Infrastructure.
//  Program.cs stays short because each layer registers its own services through an
//  extension method (AddApplication / AddInfrastructure / AddApiServices).
// =============================================================================
using SharpAPI.Api.Extensions;
using SharpAPI.Application;
using SharpAPI.Infrastructure;
using SharpAPI.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);
// CreateBuilder loads configuration in this order (later sources win):
// appsettings.json → appsettings.{Environment}.json → user-secrets (Development only)
// → environment variables → command-line args.
// That's why Docker/Kubernetes can override e.g. ConnectionStrings__Catalog with an env var.

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;                     // don't advertise "Server: Kestrel" to attackers
    kestrel.Limits.MaxRequestBodySize = 1 * 1024 * 1024; // 1 MB: a product JSON is ~300 bytes; stops oversized payload abuse
});

builder.Services
    .AddApplication()                             // use cases + validators
    .AddInfrastructure(builder.Configuration)     // EF Core, repositories, health checks
    .AddApiServices(builder.Configuration);       // ProblemDetails, CORS, caching, rate limiting, OpenAPI...

var app = builder.Build();

app.UseApiPipeline();   // middleware, in the correct order (see WebApplicationExtensions)
app.MapApiEndpoints();  // routes

// Local dev / tests on SQLite: create + seed the DB. A no-op for SQL Server (scripts own the schema).
await app.Services.InitializeDatabaseAsync();

await app.RunAsync();
