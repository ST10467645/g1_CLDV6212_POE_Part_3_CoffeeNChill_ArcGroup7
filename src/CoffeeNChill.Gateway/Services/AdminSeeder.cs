/*
 * Code Attribution:
 * Running code once at start-up with an IHostedService follows Microsoft's hosted services documentation. The retry loop
 * (try again every few seconds while storage is still starting) is my own design, needed because the gateway and
 * Azurite start at the same time in Docker Compose.
 * Creating the first Admin from configuration is my own design for the safe-registration rule (Admin-only creation of Staff and Admin).
 *
 * Microsoft Learn (2026ab). Background tasks with hosted services in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
 * [Accessed 19 October 2026].
 *
 * Microsoft Learn (2026ac). Configuration in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/
 * [Accessed 19 October 2026].
 */

using Azure;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services
{
    // Creates the first Admin account when the gateway starts, if no Admin exists yet. Retries while storage is starting.
    public class AdminSeeder : IHostedService
    {
        private const int MaxAttempts = 10;
        private static readonly TimeSpan WaitBetweenAttempts = TimeSpan.FromSeconds(3);

        private readonly UserStore _users;
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<AdminSeeder> _logger;

        public AdminSeeder(UserStore users, IConfiguration configuration, IHostApplicationLifetime lifetime, ILogger<AdminSeeder> logger)
        {
            _users = users;
            _configuration = configuration;
            _lifetime = lifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Seed in the background, so the gateway starts answering requests straight away.
            _ = Task.Run(() => SeedWithRetryAsync(_lifetime.ApplicationStopping));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private async Task SeedWithRetryAsync(CancellationToken ct)
        {
            string? email = _configuration["SeedAdmin:Email"];
            string? password = _configuration["SeedAdmin:Password"];
            string fullName = _configuration["SeedAdmin:FullName"] ?? "CoffeeNChill Admin";

            // No settings means nothing is created (for example in a test run).
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("SeedAdmin:Email or SeedAdmin:Password is not set. No Admin account was created.");
                return;
            }

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    if (await _users.AdminExistsAsync(ct))
                    {
                        _logger.LogInformation("An Admin already exists. Nothing to seed.");
                        return;
                    }

                    // The password is hashed with BCrypt before it is saved, exactly like a normal registration.
                    UserEntity admin = UserMapper.ToEntity(fullName, email, UserRoles.Admin, PasswordHasher.Hash(password));
                    await _users.AddAsync(admin, ct);

                    _logger.LogInformation("First Admin account created for {Email}.", admin.RowKey);
                    return;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    // Whne the gateway is shutting down.
                    return;
                }
                catch (RequestFailedException ex) when (ex.Status == 409)
                {
                    _logger.LogInformation("The Admin account was created by another start-up. Nothing to seed.");
                    return;
                }
                catch (Exception ex)
                {
                    // If storage is not be ready yet, there is a wait and attempts to try again.
                    _logger.LogWarning("Could not seed the Admin account (attempt {Attempt} of {Max}): {Message}", attempt, MaxAttempts, ex.Message);

                    try
                    {
                        await Task.Delay(WaitBetweenAttempts, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }

            _logger.LogError("Gave up seeding the Admin account after {Max} attempts. Is the storage (Azurite) running?", MaxAttempts);
        }
    }
}