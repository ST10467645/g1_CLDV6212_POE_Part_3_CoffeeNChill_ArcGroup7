/*
 * Code Attribution:
 * The structure of this middleware (a constructor that receives RequestDelegate and a logger, and an InvokeAsync method that calls the next middleware inside a try/catch) follows the Microsoft custom middleware page below. 
 * Microsoft's error handling documentation describes the built-in exception handler. 
 * I wrote a custom middleware instead because the POE brief asks for global exception-handling middleware.
 * The mapping of errors to status codes and messages, the safe client messages, the log-only details and the empty-body check are my own additions.
 * 
 * The kinds of errors that give a 503 (RequestFailedException, AggregateException, HttpRequestException,
 * TimeoutException, OperationCanceledException) follow the error handling in the group's Part 2 SubmitOrder
 * function (written by Kandyce). I narrowed RequestFailedException to status 0 or 5xx, so other storage errors give a 500.
 *
 * Microsoft Learn (2026av). Write custom ASP.NET Core middleware. (the RequestDelegate constructor and InvokeAsync)
 * Available at: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write
 * [22 October 2026]
 *
 * Microsoft Learn (2026aw). RequestFailedException.Status Property. (Status is 0 when no response was received)
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.requestfailedexception.status?view=azure-dotnet
 * [22 October 2026]
 *
 * Microsoft Learn (2026ax). HttpResponse.HasStarted Property. (checking that the response has not started)
 * Available at: https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpresponse.hasstarted
 * [22 October 2026]
 *
 * Microsoft Learn (2026ay). HttpResponse Class. (StatusCode, ContentType, ContentLength and WriteAsJsonAsync)
 * Available at: https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.httpresponse
 * [22 October 2026]
 *
 * Microsoft Learn (2026az). when keyword - C# reference. (the catch ... when filter)
 * Available at: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/when
 * [22 October 2026]
 * 
 * Microsoft Learn (2026at). switch expression - pattern matching expressions using the switch keyword (helped me with the switch expression)
 * Available at: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/switch-expression
 * [Accessed 22 October 2026].
 *
 * Microsoft Learn (2026ba). HttpContext.TraceIdentifier Property. (the id sent to the client and written to the log (context.TraceIdentifier))
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.traceidentifier
 * [22 October 2026]
 *
 * Microsoft Learn (2026bb). Logging in .NET Core and ASP.NET Core. (ILogger and log levels)
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0
 * [22 October 2026]
 *
 * Microsoft Learn (2026bc). HttpContext.RequestAborted Property (helped with (context.RequestAborted.IsCancellationRequested))
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.httpcontext.requestaborted?view=aspnetcore-10.0
 * [22 October 2026]
 *
 * Jovanović, M. (2025). Global Error Handling in ASP.NET Core: From Middleware to Modern Handlers.
 * (the basic try/catch middleware shape and setting the status code before writing the response)
 * Available at: https://milanjovanovic.tech/blog/global-error-handling-in-aspnetcore-from-middleware-to-modern-handlers
 * [22 October 2026]
 */
using Azure;
using CoffeeNChill.Gateway.DTOs;

namespace CoffeeNChill.Gateway.Middleware
{
    //The first middleware in the pipeline. It turns every unhandled error into a JSON answer.
    public class GlobalExceptionMiddleware
    {
        private const string NotReachable = "A service this request needs is not reachable right now. Please try again shortly.";
        private const string Unexpected = "Something went wrong on our side. Please try again and quote the traceId if it keeps happening.";

        //The next helper in the line and the logger. ASP.NET gives us both (dependency injection).
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        //ASP.NET calls this for every request.
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                //Let the rest of the pipeline (authentication, controllers ...) do what it has to.
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                //The client closed the connection. There is nobody to answer.
                _logger.LogInformation("The client cancelled {Method} {Path}.", context.Request.Method, context.Request.Path);
                return;
            }
            catch (Exception ex) when (!context.Response.HasStarted)
            {
                //Any other error as long as the answer has not started yet.
                await HandleExceptionAsync(context, ex);
                return;
            }

            //If nothing crashed but the answer is an error with no body (like a wrong address) adds a JSON body.
            if (NeedsJsonBody(context))
            {
                await WriteErrorAsync(context, context.Response.StatusCode, FriendlyText(context.Response.StatusCode));
            }
        }

        //Logs the real error (with the traceId) and sends the client a JSON message.
        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            (int status, string message) = Classify(ex);

            if (status >= 500)
            {
                _logger.LogError(ex, "Unhandled exception, answered {Status} for {Method} {Path} (traceId {TraceId}).",
                    status, context.Request.Method, context.Request.Path, context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(ex, "Request could not be read, answered {Status} for {Method} {Path} (traceId {TraceId}).",
                    status, context.Request.Method, context.Request.Path, context.TraceIdentifier);
            }

            await WriteErrorAsync(context, status, message);
        }

        //Picks the status code and message depending on the kind of error.
        private static (int Status, string Message) Classify(Exception ex)
        {
            return ex switch
            {
                //Status 0 means no answer came back at all (for example Azurite is stopped). 5xx means the storage failed.
                RequestFailedException rf when rf.Status == 0 || rf.Status >= 500 => (StatusCodes.Status503ServiceUnavailable, NotReachable),
                AggregateException or HttpRequestException or TimeoutException or OperationCanceledException
                    => (StatusCodes.Status503ServiceUnavailable, NotReachable),
                BadHttpRequestException bad => (bad.StatusCode, "The request could not be read."),
                _ => (StatusCodes.Status500InternalServerError, Unexpected)
            };
        }

        //True when an error status was set but nobody wrote a body.
        private static bool NeedsJsonBody(HttpContext context)
        {
            HttpResponse response = context.Response;
            return response.StatusCode >= 400
                   && !response.HasStarted
                   && response.ContentLength is null
                   && string.IsNullOrEmpty(response.ContentType);
        }

        private static string FriendlyText(int status)
        {
            return status switch
            {
                400 => "The request is not valid.",
                401 => "You must log in and send a valid Bearer token.",
                403 => "Your role is not allowed to use this endpoint.",
                404 => "That address does not exist on this API.",
                405 => "That HTTP method is not allowed here.",
                413 => "The request is too large.",
                415 => "The content type is not supported. Send application/json.",
                _ => status >= 500 ? "Something went wrong on our side." : "The request could not be completed."
            };
        }

        //Writes the shared ErrorResponse ({"error": "...", "traceId": "..."}) as JSON.
        private static Task WriteErrorAsync(HttpContext context, int status, string message)
        {
            context.Response.StatusCode = status;
            return context.Response.WriteAsJsonAsync(new ErrorResponse(message, context.TraceIdentifier));
        }
    }
}