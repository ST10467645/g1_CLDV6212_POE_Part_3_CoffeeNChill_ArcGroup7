namespace CoffeeNChill.Gateway.Services
{
    // STUB: Kandyce implements GenerateToken on Day 2.
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(string fullName, string email, string role)
        {
            throw new NotImplementedException("Implemented on Day 2.");
        }
    }
}