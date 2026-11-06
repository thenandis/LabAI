namespace SharpAPI.Application.Common;

/// <summary>
/// One page of results plus the metadata a UI needs to draw pagination controls.
/// </summary>
/// <remarks>
/// WHY PAGING: never return an unbounded list from an API. 77 products is fine;
/// 7 million is an outage (memory, network, JSON serialisation, browser rendering).
/// The database does the paging with OFFSET/FETCH, so only one page crosses the wire.
/// <para>
/// Interview extra: OFFSET paging slows down on very deep pages (the DB still scans
/// the skipped rows). For infinite-scroll feeds at scale, use KEYSET (cursor) paging:
/// <c>WHERE Id &gt; @lastSeenId ORDER BY Id</c>.
/// </para>
/// </remarks>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
