namespace SharpAPI.Api.Middleware;

/// <summary>
/// Adds standard security headers to every response (OWASP Secure Headers recommendations).
/// </summary>
/// <remarks>
/// A convention-based middleware: the constructor takes the next delegate, and
/// <c>InvokeAsync</c> runs once per request. Code before <c>await next(context)</c> runs on
/// the way IN; code after it would run on the way OUT.
/// </remarks>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers.XContentTypeOptions = "nosniff";              // stop MIME-type sniffing attacks
        headers.XFrameOptions = "DENY";                       // stop click-jacking via <iframe>
        headers["Referrer-Policy"] = "no-referrer";
        headers["Cross-Origin-Resource-Policy"] = "same-site";

        // JSON API responses should never execute anything. The /scalar docs UI needs
        // scripts, so the strict CSP applies only to /api.
        if (context.Request.Path.StartsWithSegments("/api"))
            headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

        return next(context);
    }
}
