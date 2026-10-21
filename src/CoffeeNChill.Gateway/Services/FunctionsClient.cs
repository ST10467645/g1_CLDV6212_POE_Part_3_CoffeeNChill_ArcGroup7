/*
 * Code Attribution:
 * The typed HttpClient (a class that receives an HttpClient from the factory and wraps calls to one other service) follows
 * Microsoft's IHttpClientFactory documentation. Reading JSON with JsonSerializer follows Microsoft's System.Text.Json
 * documentation (the same method used in my Part 2 functions).
 *
 * Microsoft Learn (2026aa). Make HTTP requests using IHttpClientFactory in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026s). JsonSerializer.Deserialize Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserialize?view=net-10.0
 * [Accessed 19 October 2026].
 */

using System.Net;
using System.Text.Json;
using CoffeeNChill.Gateway.DTOs;

namespace CoffeeNChill.Gateway.Services
{
    // The answer from the Functions app: the HTTP status, the data when it worked, or an error text when it did not.
    public record FunctionsResult<T>(HttpStatusCode StatusCode, T? Data, string? Error)
    {
        public bool IsSuccess => (int)StatusCode >= 200 && (int)StatusCode < 300;
    }

    // Talks to the Part 2 Azure Functions app (order list, order lookup, order status, status update).
    public class FunctionsClient
    {
        // Web defaults: the property names are not case sensitive, so "OrderId" and "orderId" both work.
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;

        public FunctionsClient(HttpClient http)
        {
            _http = http;
        }

        // GET orders or GET orders?status=Received
        public Task<FunctionsResult<List<OrderResponse>>> GetOrdersAsync(string? status, CancellationToken ct = default)
        {
            string url = string.IsNullOrWhiteSpace(status) ? "orders" : $"orders?status={Uri.EscapeDataString(status)}";
            return SendAsync<List<OrderResponse>>(new HttpRequestMessage(HttpMethod.Get, url), ct);
        }

        // GET orders/{orderDate}/{orderId}
        public Task<FunctionsResult<OrderResponse>> GetOrderAsync(string orderDate, string orderId, CancellationToken ct = default)
        {
            string url = $"orders/{Uri.EscapeDataString(orderDate)}/{Uri.EscapeDataString(orderId)}";
            return SendAsync<OrderResponse>(new HttpRequestMessage(HttpMethod.Get, url), ct);
        }

        // GET orders/{orderId}  (only the id and the status)
        public Task<FunctionsResult<OrderStatusResponse>> GetOrderStatusAsync(string orderId, CancellationToken ct = default)
        {
            string url = $"orders/{Uri.EscapeDataString(orderId)}";
            return SendAsync<OrderStatusResponse>(new HttpRequestMessage(HttpMethod.Get, url), ct);
        }

        // PUT orders/{orderDate}/{orderId}/status with the body {"Status":"Preparing"}
        public Task<FunctionsResult<OrderResponse>> UpdateStatusAsync(string orderDate, string orderId, string status, CancellationToken ct = default)
        {
            string url = $"orders/{Uri.EscapeDataString(orderDate)}/{Uri.EscapeDataString(orderId)}/status";
            var request = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(new { Status = status })
            };
            return SendAsync<OrderResponse>(request, ct);
        }

        // Sends the request, reads the answer and turns it into a FunctionsResult.
        // If the Functions app cannot be reached at all, the HttpRequestException is not caught here.
        // The global exception middleware turns it into a 503.
        private async Task<FunctionsResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
        {
            using HttpResponseMessage response = await _http.SendAsync(request, ct);
            string body = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                T? data = JsonSerializer.Deserialize<T>(body, JsonOptions);
                return new FunctionsResult<T>(response.StatusCode, data, null);
            }

            return new FunctionsResult<T>(response.StatusCode, default, ReadError(body, response));
        }

        // The Functions send errors as {"error":"..."}. Pull the text out, or fall back to a short version of the body.
        private static string ReadError(string body, HttpResponseMessage response)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("error", out JsonElement error) &&
                    error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString() ?? "The orders service returned an error.";
                }
            }
            catch (JsonException)
            {
                // The body was not JSON. Use the fallback below.
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return response.ReasonPhrase ?? "The orders service returned an error.";
            }

            return body.Length > 200 ? body[..200] : body;
        }
    }
}