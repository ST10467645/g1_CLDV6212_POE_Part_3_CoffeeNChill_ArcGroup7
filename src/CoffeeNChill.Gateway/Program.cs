/*
 * Code Attribution:
 * The JWT bearer authentication setup (TokenValidationParameters with issuer, audience,
 * lifetime and signing key checks) and the Swagger "Authorize" button for a Bearer token
 * were adapted from the lecturer-provided CLDV6212 Student Relations gateway example and
 * from the PROG6212 lecturer-provided Campus Library API example (IIE, 2026), together with
 * Microsoft's official ASP.NET Core documentation.
 * The middleware pipeline order, the typed HttpClient registration, the configuration
 * binding and the hosted service registration follow Microsoft's documentation.
 *
 * Microsoft Learn (2026x). Configure JWT bearer authentication in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026y). Write custom ASP.NET Core middleware. [Online]
 * Available at: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026z). Get started with Swashbuckle and ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026aa). Make HTTP requests using IHttpClientFactory in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026ab). Background tasks with hosted services in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026ac). Configuration in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
 *
 * Microsoft Learn (2026ae). Create web APIs with ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/
 * [Accessed 20 October 2026].
 */

using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Middleware;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------- Storage clients (Azurite locally and on the VPS) ----------
// The connection string comes from configuration. In Docker it points to the "azurite" service.
string storage = builder.Configuration["AzureStorageConnectionString"] ?? "UseDevelopmentStorage=true";

// Fail fast when storage is down (same idea as Part 2): 1 retry, 5 second timeout, so the client is not left waiting.
var tableOptions = new TableClientOptions();
tableOptions.Retry.MaxRetries = 1;
tableOptions.Retry.Delay = TimeSpan.FromMilliseconds(500);
tableOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(5);

var blobOptions = new BlobClientOptions();
blobOptions.Retry.MaxRetries = 1;
blobOptions.Retry.Delay = TimeSpan.FromMilliseconds(500);
blobOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(5);

// MessageEncoding.None: we do the Base64 step ourselves in OrdersController, exactly like Part 2.
var queueOptions = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.None };
queueOptions.Retry.MaxRetries = 1;
queueOptions.Retry.Delay = TimeSpan.FromMilliseconds(500);
queueOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(5);

builder.Services.AddSingleton(new TableServiceClient(storage, tableOptions));
builder.Services.AddSingleton(new BlobServiceClient(storage, blobOptions));
builder.Services.AddSingleton(new QueueServiceClient(storage, queueOptions));

// ---------- Our own services ----------
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddHostedService<AdminSeeder>();   // creates the first Admin at start-up

// Typed HttpClient that forwards order requests to the Azure Functions app.
builder.Services.AddHttpClient<FunctionsClient>(client =>
{
    string baseUrl = builder.Configuration["Functions:BaseUrl"] ?? "http://localhost:7071/api";
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Every automatic 400 (a missing or bad field) comes back in our ValidationErrorResponse shape, all errors in one list.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value != null && entry.Value.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(error =>
                    string.IsNullOrWhiteSpace(error.ErrorMessage) ? $"'{entry.Key}' is not valid." : error.ErrorMessage))
                .ToList();

            return new BadRequestObjectResult(new ValidationErrorResponse(errors));
        };
    });

// ---------- JWT authentication ----------
string? jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    // HS256 needs a long secret. This stops the app early with a clear message instead of failing at login time.
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Set it in appsettings.Development.json or the JWT__KEY environment variable.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // Return our JSON error shape for 401 and 403 instead of an empty body.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ErrorResponse(
                    "You must log in and send a valid Bearer token.", context.HttpContext.TraceIdentifier));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ErrorResponse(
                    "Your role is not allowed to use this endpoint.", context.HttpContext.TraceIdentifier));
            }
        };
    });

builder.Services.AddAuthorization();

// ---------- Swagger / OpenAPI with XML comments and the Authorize button ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CoffeeNChill API Gateway",
        Version = "v1 - deployed by GitHub Actions",
        Description = "Single secure entry point for CoffeeNChill. Public: view the menu, place an order, check an order status. " +
                      "Staff and Admin: change the menu, manage documents, update order statuses. " +
                      "Log in with POST /api/auth/login, then click Authorize and paste the token."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste ONLY the token (no quotes, no the word Bearer). Swagger adds 'Bearer ' for you."
    });

    options.AddSecurityRequirement(document =>
    new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] =
            new List<string>()
    });

    // Read the XML comments (///) that the build generates, so every endpoint is described in Swagger.
    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ---------- Pipeline. ORDER MATTERS ----------
app.UseMiddleware<GlobalExceptionMiddleware>();   // 1. catches every unhandled exception below it

// Swagger is switched on by configuration (Swagger:Enabled) so it also works in Production on the VPS.
if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseAuthentication();   // 2. who are you (reads the token). MUST come before authorization
app.UseAuthorization();    // 3. what may you do (checks roles)

app.MapControllers();

app.Run();

// Lets the test project start the app later.
public partial class Program { }