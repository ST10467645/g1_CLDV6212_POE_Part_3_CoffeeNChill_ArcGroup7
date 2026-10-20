/*
 * Code Attribution:
 * Custom middleware structure follows Microsoft's ASP.NET Core middleware documentation.
 *
 * Microsoft Learn (2026y). ASP.NET Core middleware. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/
 * [Accessed 19 October 2026].
 */

namespace CoffeeNChill.Gateway.Middleware
{
    // STUB: passes the request on. Kristen replaces the body with try/catch and JSON error handling.
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
        }
    }
}