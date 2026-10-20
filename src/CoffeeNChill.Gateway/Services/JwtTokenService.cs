/*
 * Code Attribution:
 * Building the claims (Name, Email, Role), signing them with HmacSha256 and writing the token was adapted from the
 * lecturer-provided CLDV6212 Student Relations gateway example and from the JwtService idea in the PROG6212
 * lecturer-provided Campus Library API example (IIE, 2026).
 *
 * Microsoft Learn (2026ah). JwtSecurityToken Class. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.identitymodel.tokens.jwt.jwtsecuritytoken
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026x). Configure JWT bearer authentication in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
 * [Accessed 19 October 2026].
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChill.Gateway.Services
{
    // Creates the signed JWT that a person receives when they log in.
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // How many hours a token lives. Comes from Jwt:ExpiryHours, 2 if it is missing.
        public int ExpiryHours => _configuration.GetValue<int?>("Jwt:ExpiryHours") ?? 2;

        // Builds and signs a token. The token proves who the person is (Name, Email) and what they may do (Role).
        public string GenerateToken(string fullName, string email, string role)
        {
            string? key = _configuration["Jwt:Key"];

            // HS256 needs a long secret, therefore, it stops early with a clear message instead of a confusing crypto error.
            if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            {
                throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters.");
            }

            // Creating the claims (the facts stored inside the token).Can only be read, cant be changed without breaking the signature.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            // The signing key and the algorithm (HmacSha256) create the signature at the end of the token.
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(ExpiryHours),
                signingCredentials: credentials);

            // Turns the token object into the "header.payload.signature" text.
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}