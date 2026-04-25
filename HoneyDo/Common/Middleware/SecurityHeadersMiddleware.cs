namespace HoneyDo.Common.Middleware;

/// <summary>
/// Adds security-hardening HTTP response headers to every response.
///
/// Registered first in the pipeline so the headers appear even on responses
/// written by later middleware (e.g. ExceptionMiddleware error payloads).
///
/// Headers applied:
///   X-Frame-Options: DENY
///     Prevents the app from being embedded in a frame or iframe, blocking
///     clickjacking attacks (CodeQL: CWE-451 / CWE-829).
///
///   X-Content-Type-Options: nosniff
///     Tells browsers not to MIME-sniff responses away from the declared
///     Content-Type, preventing content-injection attacks.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        return next(context);
    }
}
