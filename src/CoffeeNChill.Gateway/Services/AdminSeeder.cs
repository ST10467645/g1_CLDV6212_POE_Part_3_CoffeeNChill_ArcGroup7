namespace CoffeeNChill.Gateway.Services
{
    // STUB: does nothing yet. Kandyce adds the "create first Admin" logic on Day 3.
    public class AdminSeeder : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}