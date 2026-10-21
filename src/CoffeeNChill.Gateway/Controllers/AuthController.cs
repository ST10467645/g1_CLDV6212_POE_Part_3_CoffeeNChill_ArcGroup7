/*
 * Code Attribution:
 * The register and login flow (find the user, hash on register, Verify on login, return a JWT) was adapted from the
 * lecturer-provided CLDV6212 Student Relations gateway example and the PROG6212 lecturer-provided Campus Library API
 * example (IIE, 2026). BCrypt replaces the SHA256 hash used in the PROG6212 examples. The rule that only an Admin can
 * create Staff or Admin accounts is my own design for strict role-based access control.
 * The controller attributes and return types follow Microsoft's ASP.NET Core web API documentation.
 *
 * Microsoft Learn (2026ae). Create web APIs with ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026af). Role-based authorization in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026ai). Controller action return types in ASP.NET Core web API. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types
 * [Accessed 20 October 2026].
 */

using Azure;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    /// <summary>
    /// Register a new account and log in to get a JWT token.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly UserStore _users;
        private readonly JwtTokenService _jwt;

        public AuthController(UserStore users, JwtTokenService jwt)
        {
            _users = users;
            _jwt = jwt;
        }

        /// <summary>
        /// Registers a new account. Anyone can register, but without an Admin token the account is always a Customer.
        /// An Admin token is needed to create Staff or Admin accounts.
        /// </summary>
        /// <param name="request">Full name, email, password and (Admin only) the role.</param>
        /// <response code="201">The account was created.</response>
        /// <response code="400">A field is missing or not valid.</response>
        /// <response code="409">That email is already registered.</response>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserResponse>> Register([FromBody] RegisterRequest request)
        {
            // Everybody starts as a Customer.
            string role = UserRoles.Customer;

            // Only a caller with a valid Admin token may choose another role.
            if (User.IsInRole(UserRoles.Admin) && !string.IsNullOrWhiteSpace(request.Role))
            {
                string? requested = UserRoles.Normalise(request.Role);
                if (requested == null)
                {
                    return BadRequest(new ValidationErrorResponse("Role must be Admin, Staff or Customer."));
                }

                role = requested;
            }

            // The email must not exist in any role.
            if (await _users.FindByEmailAsync(request.Email) != null)
            {
                return Conflict(new ErrorResponse("That email is already registered.", HttpContext.TraceIdentifier));
            }

            // Only the BCrypt hash is stored, never the password.
            UserEntity entity = UserMapper.ToEntity(request.FullName, request.Email, role, PasswordHasher.Hash(request.Password));

            try
            {
                await _users.AddAsync(entity);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                // If two people registered the same email at the same moment.
                return Conflict(new ErrorResponse("That email is already registered.", HttpContext.TraceIdentifier));
            }

            return StatusCode(StatusCodes.Status201Created, UserMapper.ToResponse(entity));
        }

        /// <summary>
        /// Logs in with email and password and returns a signed JWT that is valid for 2 hours.
        /// </summary>
        /// <param name="request">Email and password.</param>
        /// <response code="200">Login worked. Use the token as: Authorization: Bearer (token).</response>
        /// <response code="400">Email or password is missing.</response>
        /// <response code="401">Wrong email or password.</response>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            UserEntity? user = await _users.FindByEmailAsync(request.Email);

            // Same message for "no such email" and "wrong password", so nobody can find out which emails exist.
            if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new ErrorResponse("Invalid email or password.", HttpContext.TraceIdentifier));
            }

            // The PartitionKey of the row is the role.
            string token = _jwt.GenerateToken(user.FullName, user.RowKey, user.PartitionKey);

            return Ok(new LoginResponse
            {
                Token = token,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(_jwt.ExpiryHours),
                FullName = user.FullName,
                Email = user.RowKey,
                Role = user.PartitionKey
            });
        }
    }
}