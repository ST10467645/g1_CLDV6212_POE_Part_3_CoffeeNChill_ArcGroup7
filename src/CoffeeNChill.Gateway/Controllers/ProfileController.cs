/*
 * Code Attribution:
 * The "mine" idea (the controller reads who is calling from the token claims and never from the URL, so a user can only
 * see and change their own profile) was adapted from the ProfileController in the lecturer-provided PROG6212 examples
 * (IIE, 2026). The controller attributes, validation and role rules follow Microsoft's ASP.NET Core documentation.
 *
 * Microsoft Learn (2026ap). ClaimsPrincipal.FindFirst Method. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimsprincipal.findfirst?view=net-10.0
 * [Accessed 22 October 2026].
 *
 * Microsoft Learn (2026ae). Create web APIs with ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/
 * [Accessed 20 October 2026].
 *
 * Microsoft Learn (2026af). Role-based authorization in ASP.NET Core. [Online]
 * Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
 * [Accessed 20 October 2026].
 */

using System.Security.Claims;
using Azure;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    /// <summary>
    /// The logged-in person's own profile. Any role (Customer, Staff, Admin) may use it. The person is read from the token.
    /// </summary>
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    [Produces("application/json")]
    public class ProfileController : ControllerBase
    {
        private readonly UserStore _users;

        public ProfileController(UserStore users)
        {
            _users = users;
        }

        /// <summary>
        /// Shows my own profile (name, email, role, created date). The password hash is never returned.
        /// </summary>
        /// <response code="200">My profile.</response>
        /// <response code="401">No token, a bad token, or the account no longer exists.</response>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UserResponse>> GetMine(CancellationToken ct)
        {
            UserEntity? user = await FindCallerAsync(ct);
            if (user is null)
            {
                return AccountGone();
            }

            return Ok(UserMapper.ToResponse(user));
        }

        /// <summary>
        /// Changes my name, my password, or both. To set a new password the current password must be sent too.
        /// </summary>
        /// <param name="request">FullName and/or NewPassword (with CurrentPassword).</param>
        /// <response code="200">The updated profile.</response>
        /// <response code="400">Nothing to change, a field is not valid, or the current password is wrong.</response>
        /// <response code="401">No token, a bad token, or the account no longer exists.</response>
        /// <response code="409">The profile was changed somewhere else at the same moment. Try again.</response>
        /// <param name="ct">Token used to cancel the request.</param>
        [HttpPut("mine")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<UserResponse>> UpdateMine([FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            bool changeName = !string.IsNullOrWhiteSpace(request.FullName);
            bool changePassword = !string.IsNullOrWhiteSpace(request.NewPassword);

            if (!changeName && !changePassword)
            {
                return BadRequest(new ValidationErrorResponse("Send FullName, NewPassword or both."));
            }

            if (changePassword && string.IsNullOrWhiteSpace(request.CurrentPassword))
            {
                return BadRequest(new ValidationErrorResponse("CurrentPassword is required to set a new password."));
            }

            UserEntity? user = await FindCallerAsync(ct);
            if (user is null)
            {
                return AccountGone();
            }

            if (changePassword)
            {
                // Checking  to see if the typed current password  matches the stored hash before accepting a new one.
                if (!PasswordHasher.Verify(request.CurrentPassword!, user.PasswordHash))
                {
                    return BadRequest(new ValidationErrorResponse("CurrentPassword is wrong."));
                }

                user.PasswordHash = PasswordHasher.Hash(request.NewPassword!);
            }

            if (changeName)
            {
                user.FullName = request.FullName!.Trim();
            }

            try
            {
                await _users.UpdateAsync(user, ct);
            }
            catch (RequestFailedException ex) when (ex.Status == 412)
            {
                // If the row changed after it was read (The ETag did not match).
                return Conflict(new ErrorResponse("Your profile was changed somewhere else. Please try again.", HttpContext.TraceIdentifier));
            }

            return Ok(UserMapper.ToResponse(user));
        }

        // Finds the Users row of the person who is calling, using the email claim inside the token.
        private async Task<UserEntity?> FindCallerAsync(CancellationToken ct)
        {
            string? email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return await _users.FindByEmailAsync(email, ct);
        }

        // This if for when token is valid but the account was deleted after the token was made.
        private ActionResult AccountGone()
        {
            return Unauthorized(new ErrorResponse("This account no longer exists. Please log in again.", HttpContext.TraceIdentifier));
        }
    }
}