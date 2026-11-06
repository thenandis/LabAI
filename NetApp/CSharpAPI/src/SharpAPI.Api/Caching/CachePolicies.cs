namespace SharpAPI.Api.Caching;

/// <summary>
/// Output-cache policy names and tags, as constants so a typo can't silently break eviction.
/// </summary>
/// <remarks>
/// A POLICY decides how a response is cached (duration, tags). A TAG groups cache entries,
/// so one call (<c>EvictByTagAsync("products")</c>) purges every cached product list
/// page and detail at once after a write.
/// </remarks>
internal static class CachePolicies
{
    public const string Products = "products-policy";
    public const string ProductsTag = "products";

    public const string Lookups = "lookups-policy";
    public const string LookupsTag = "lookups";
}
