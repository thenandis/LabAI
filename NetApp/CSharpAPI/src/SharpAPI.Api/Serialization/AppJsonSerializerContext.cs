using System.Text.Json;
using System.Text.Json.Serialization;
using SharpAPI.Application.Common;
using SharpAPI.Application.Lookups;
using SharpAPI.Application.Products;

namespace SharpAPI.Api.Serialization;

/// <summary>
/// System.Text.Json SOURCE GENERATOR context. At build time the compiler generates
/// optimised (de)serialisation code for each listed type, with no runtime reflection.
/// </summary>
/// <remarks>
/// Benefits: faster cold start, higher throughput, less memory, and compatibility with
/// Native AOT / trimming. <see cref="JsonSerializerDefaults.Web"/> = camelCase names +
/// case-insensitive reading, the same as ASP.NET Core's defaults.
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(PagedResult<ProductDto>))]
[JsonSerializable(typeof(CreateProductRequest))]
[JsonSerializable(typeof(UpdateProductRequest))]
[JsonSerializable(typeof(IReadOnlyList<LookupDto>))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
