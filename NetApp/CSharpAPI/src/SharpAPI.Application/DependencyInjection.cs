using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SharpAPI.Application.Products;

namespace SharpAPI.Application;

/// <summary>
/// Each layer registers its OWN services. Program.cs just calls
/// <c>builder.Services.AddApplication()</c> and doesn't need to know what's inside.
/// This keeps the composition root tidy and respects encapsulation (ProductService is
/// internal, so only this assembly can register it).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // DI LIFETIMES (a classic interview question):
        //  - Transient: new instance every time it's resolved.
        //  - Scoped:    one instance per HTTP request. Right for anything that uses the
        //               DbContext, which is itself scoped and NOT thread-safe.
        //  - Singleton: one instance for the app's lifetime. Must be thread-safe and
        //               must never capture a scoped dependency ("captive dependency" bug).
        services.AddScoped<IProductService, ProductService>();

        // Scans this assembly and registers every AbstractValidator<T> as IValidator<T>.
        // Scoped because some validators use ILookupQueries (which uses the DbContext).
        services.AddValidatorsFromAssembly(
            typeof(DependencyInjection).Assembly,
            ServiceLifetime.Scoped,
            includeInternalTypes: true);

        return services;
    }
}
