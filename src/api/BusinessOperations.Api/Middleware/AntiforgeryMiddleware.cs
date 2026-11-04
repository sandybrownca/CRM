using Microsoft.AspNetCore.Antiforgery;

namespace BusinessOperations.Api.Middleware;

public sealed class AntiforgeryValidationMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS", "TRACE"
    };

    private readonly RequestDelegate _next;

    public AntiforgeryValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Login and CSRF token issuance must remain reachable without a prior token.
        var isExempt =
            path.Equals("/api/v1/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/v1/auth/csrf", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/health", StringComparison.OrdinalIgnoreCase);

        if (!SafeMethods.Contains(context.Request.Method) && !isExempt)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "CSRF validation failed",
                    detail = "A valid X-CSRF-TOKEN header is required for this request.",
                    status = 400
                });
                return;
            }
        }

        await _next(context);
    }
}
