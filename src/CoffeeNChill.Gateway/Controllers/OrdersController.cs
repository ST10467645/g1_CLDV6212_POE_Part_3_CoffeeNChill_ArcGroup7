/*
 * Code Attribution:
 * The queue producer part (validate, serialise to JSON, encode as Base64 in code, send with QueueClient, 202 Accepted, 503 when
 * the queue is unreachable) was adapted from my Part 2 SubmitOrder function, which was based on the lecturer-provided
 * ProcessStudentQuery / queue examples (IIE, 2026). The routing of the other endpoints to the Functions app uses a typed
 * HttpClient. The controller attributes and role rules follow Microsoft's ASP.NET Core documentation.
 *
 * Microsoft Learn (2026q). Azure Queue Storage client library for .NET. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/storage.queues-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 *
 * Microsoft Learn (2026w). Convert.ToBase64String Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.convert.tobase64string?view=net-10.0
 * [Accessed 09 October 2026].
 *
 * Microsoft Learn (2026r). RequestFailedException Class. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.requestfailedexception?view=azure-dotnet
 * [Accessed 01 October 2026].
 *
 * Microsoft Learn (2026aa). Make HTTP requests using IHttpClientFactory in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026ae). Create web APIs with ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026af). Role-based authorization in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
 * [Accessed 20 October 2026].
 */

using System.Net;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Storage.Queues;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    /// <summary>
    /// Place orders and follow them. Anyone can place an order and check a status. Admin and Staff manage orders.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        // Must be the same name that the ProcessOrderQueue function listens to.
        private const string QueueName = "order-processing-queue";

        private static readonly string[] Statuses = { "Received", "Preparing", "Ready", "Collected" };

        private readonly QueueServiceClient _queues;
        private readonly FunctionsClient _functions;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(QueueServiceClient queues, FunctionsClient functions, ILogger<OrdersController> logger)
        {
            _queues = queues;
            _functions = functions;
            _logger = logger;
        }

        /// <summary>
        /// Places a new order on the queue. No login needed. The order is processed in the background.
        /// </summary>
        /// <param name="request">Customer name, the SKUs of the menu items and the total price.</param>
        /// <response code="202">The order was accepted. Keep the orderId to check the status.</response>
        /// <response code="400">One or more fields are missing or not valid.</response>
        /// <response code="503">The queue cannot be reached right now.</response>
        [HttpPost("queue")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderQueuedResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<OrderQueuedResponse>> PlaceOrder([FromBody] CreateOrderRequest request)
        {
            // The attribute checks ran already. This one checks each SKU in the list.
            if (request.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace))
            {
                return BadRequest(new ValidationErrorResponse("SelectedItemSKUs cannot contain blank values."));
            }

            OrderQueueMessage message = OrderMapper.ToQueueMessage(request);

            // The queue message is plain text: JSON first, then Base64 (the same as Part 2, host.json says messageEncoding none).
            string json = JsonSerializer.Serialize(message);
            string base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            try
            {
                QueueClient queue = _queues.GetQueueClient(QueueName);
                await queue.CreateIfNotExistsAsync();
                await queue.SendMessageAsync(base64);
            }
            catch (Exception ex) when (ex is RequestFailedException || ex is AggregateException || ex is HttpRequestException ||
                                       ex is TimeoutException || ex is OperationCanceledException)
            {
                // Azurite or the queue is down: tell the client to try again, do not crash.
                _logger.LogError(ex, "Queue storage is not reachable.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new ErrorResponse("The order queue is not reachable. Please try again shortly.", HttpContext.TraceIdentifier));
            }

            _logger.LogInformation("Order {OrderId} placed on {Queue}.", message.OrderId, QueueName);
            return Accepted(OrderMapper.ToQueuedResponse(message));
        }

        /// <summary>
        /// Checks the status of one order (Received, Preparing, Ready or Collected). No login needed.
        /// </summary>
        /// <param name="orderId">The order number, for example ORD-2026-A1B2C3.</param>
        /// <response code="200">The order id and its current status.</response>
        /// <response code="404">There is no order with that id.</response>
        [HttpGet("{orderId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(OrderStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderStatusResponse>> GetStatus(string orderId)
        {
            FunctionsResult<OrderStatusResponse> result = await _functions.GetOrderStatusAsync(orderId);
            return result.IsSuccess ? Ok(result.Data) : MapFailure(result);
        }

        /// <summary>
        /// Lists all orders. Optional filter ?status=Received. Admin and Staff only.
        /// </summary>
        /// <param name="status">Optional: Received, Preparing, Ready or Collected.</param>
        /// <response code="200">The list of orders (an empty list when there are none).</response>
        /// <response code="400">The status filter is not one of the four statuses.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<OrderResponse>>> GetOrders([FromQuery] string? status)
        {
            if (!string.IsNullOrWhiteSpace(status) && !Statuses.Contains(status))
            {
                return BadRequest(new ValidationErrorResponse("Status must be one of: Received, Preparing, Ready, Collected."));
            }

            FunctionsResult<List<OrderResponse>> result = await _functions.GetOrdersAsync(status);
            return result.IsSuccess ? Ok(result.Data) : MapFailure(result);
        }

        /// <summary>
        /// Gets one full order. Admin and Staff only.
        /// </summary>
        /// <param name="orderDate">The order date, for example 2026-10-21 (it is the PartitionKey).</param>
        /// <param name="orderId">The order number.</param>
        /// <response code="200">The order.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        /// <response code="404">There is no such order.</response>
        [HttpGet("{orderDate}/{orderId}")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> GetOrder(string orderDate, string orderId)
        {
            FunctionsResult<OrderResponse> result = await _functions.GetOrderAsync(orderDate, orderId);
            return result.IsSuccess ? Ok(result.Data) : MapFailure(result);
        }

        /// <summary>
        /// Moves an order to the next status. Only the next step is allowed (Received, Preparing, Ready, Collected). Admin and Staff only.
        /// </summary>
        /// <param name="orderDate">The order date, for example 2026-10-21.</param>
        /// <param name="orderId">The order number.</param>
        /// <param name="request">The new status.</param>
        /// <response code="200">The order with its new status.</response>
        /// <response code="400">The status is not valid or is not the next step.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin or Staff.</response>
        /// <response code="404">There is no such order.</response>
        [HttpPut("{orderDate}/{orderId}/status")]
        [Authorize(Roles = "Admin,Staff")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderResponse>> UpdateStatus(string orderDate, string orderId, [FromBody] StatusUpdateRequest request)
        {
            FunctionsResult<OrderResponse> result = await _functions.UpdateStatusAsync(orderDate, orderId, request.Status);
            return result.IsSuccess ? Ok(result.Data) : MapFailure(result);
        }

        // Turns a failed answer from the Functions into our JSON error shape.
        private ActionResult MapFailure<T>(FunctionsResult<T> result)
        {
            string message = result.Error ?? "The orders service returned an error.";

            return result.StatusCode switch
            {
                HttpStatusCode.NotFound => NotFound(new ErrorResponse(message, HttpContext.TraceIdentifier)),
                HttpStatusCode.BadRequest => BadRequest(new ErrorResponse(message, HttpContext.TraceIdentifier)),
                _ => StatusCode(StatusCodes.Status503ServiceUnavailable,
                        new ErrorResponse("The orders service is not available right now. Please try again shortly.", HttpContext.TraceIdentifier))
            };
        }
    }
}