/*
 * Code Attribution:
 * The error response DTO pattern follows my Part 2 ErrorResponse class, which was based on the
 * Azure Functions HTTP trigger documentation, and the ASP.NET Core error handling guidance.
 *
 * Microsoft Learn (2025b). Azure Functions HTTP trigger. [Online]
 * Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=python-v2%2Cisolated-process%2Cnodejsv4%2Cfunctionsv2&pivots=programming-language-csharp
 * [Accessed 04 September 2026].
 */

using System.Text.Json.Serialization;

namespace CoffeeNChill.Gateway.DTOs
{
    // Defines the DTO that is returned for any single error (401, 403, 404, 500, 503 and so on).
    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        // Lets us find the matching line in the log.
        [JsonPropertyName("traceId")]
        public string? TraceId { get; set; }

        public ErrorResponse() { }

        public ErrorResponse(string error, string? traceId = null)
        {
            Error = error;
            TraceId = traceId;
        }
    }

    // Defines the DTO that lists every validation problem together in one 400 Bad Request.
    public class ValidationErrorResponse
    {
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        public ValidationErrorResponse() { }

        public ValidationErrorResponse(string error) { Errors = new List<string> { error }; }

        public ValidationErrorResponse(List<string> errors) { Errors = errors; }
    }
}