/*
 * Code Attribution:
 * Running code once at start-up with an IHostedService follows Microsoft's hosted services documentation.
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

using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services
{
    // Creates the first Admin account when the gateway starts, if no Admin exists yet.
    public class AdminSeeder : IHostedService
    {
        private readonly UserStore _users;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminSeeder> _logger;

        public AdminSeeder(UserStore users, IConfiguration configuration, ILogger<AdminSeeder> logger)
        {
            _users = users;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            string? email = _configuration["SeedAdmin:Email"];
            string? password = _configuration["SeedAdmin:Password"];
            string fullName = _configuration["SeedAdmin:FullName"] ?? "CoffeeNChill Admin";

            // No settings is for not creating anything (like in a test run).
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("SeedAdmin:Email or SeedAdmin:Password is not set. No Admin account was created.");
                return;
            }

            try
            {
                if (await _users.AdminExistsAsync(cancellationToken))
                {
                    _logger.LogInformation("An Admin already exists. Nothing to seed.");
                    return;
                }

                // The password is hashed with BCrypt before it is saved, exactly like a normal registration.
                UserEntity admin = UserMapper.ToEntity(fullName, email, UserRoles.Admin, PasswordHasher.Hash(password));
                await _users.AddAsync(admin, cancellationToken);

                _logger.LogInformation("First Admin account created for {Email}.", admin.RowKey);
            }
            catch (Exception ex)
            {
                // Storage may not be running yet so it gets logged and the gateway starts anyway.
                _logger.LogError(ex, "Could not seed the Admin account. Is the storage (Azurite) running?");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}