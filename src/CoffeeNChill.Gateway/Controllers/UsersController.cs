/*
 * Code Attribution:
 * The Admin-only user list and delete (with the role check on the whole controller) was adapted from the UsersController in
 * the lecturer-provided PROG6212 examples (IIE, 2026). The query by role uses the PartitionKey of the Users table
 * (Microsoft Learn, 2026c). The attributes and role rules follow Microsoft's ASP.NET Core documentation.
 *
 * Microsoft Learn (2026c). Azure Tables client library for .NET - version 12.12.0. [Online]
 * Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet
 * [Accessed 04 September 2026].
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
    /// User management. Admin only.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = UserRoles.Admin)]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly UserStore _users;

        public UsersController(UserStore users)
        {
            _users = users;
        }

        /// <summary>
        /// Lists all users, or only one role with ?role=Staff. The password hash is never returned.
        /// </summary>
        /// <param name="role">Optional: Admin, Staff or Customer.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <response code="200">The users (an empty list when there are none).</response>
        /// <response code="400">The role is not Admin, Staff or Customer.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<List<UserResponse>>> List([FromQuery] string? role, CancellationToken ct)
        {
            string? partition = null;

            if (!string.IsNullOrWhiteSpace(role))
            {
                partition = UserRoles.Normalise(role);
                if (partition is null)
                {
                    return BadRequest(new ValidationErrorResponse("Role must be Admin, Staff or Customer."));
                }
            }

            List<UserEntity> users = await _users.ListAsync(partition, ct);

            List<UserResponse> response = users
                .OrderBy(u => u.PartitionKey)
                .ThenBy(u => u.FullName)
                .Select(UserMapper.ToResponse)
                .ToList();

            return Ok(response);
        }

        /// <summary>
        /// Deletes a user by email. An Admin cannot delete their own account.
        /// </summary>
        /// <param name="email">The email address of the user to delete.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <response code="204">The user was deleted.</response>
        /// <response code="400">You tried to delete your own account.</response>
        /// <response code="401">No token, or the token is not valid.</response>
        /// <response code="403">The token is valid but the role is not Admin.</response>
        /// <response code="404">There is no user with that email.</response>
        [HttpDelete("{email}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string email, CancellationToken ct)
        {
            string target = email.Trim().ToLowerInvariant();
            string? caller = User.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLowerInvariant();

            // Stops the last Admin from being removed by accident.
            if (target == caller)
            {
                return BadRequest(new ErrorResponse("You cannot delete your own account.", HttpContext.TraceIdentifier));
            }

            UserEntity? user = await _users.FindByEmailAsync(target, ct);
            if (user is null)
            {
                return NotFound(new ErrorResponse("No user with that email.", HttpContext.TraceIdentifier));
            }

            try
            {
                await _users.DeleteAsync(user, ct);
            }

            // Checking if somebody deleted it between the lookup and the delete.
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return NotFound(new ErrorResponse("No user with that email.", HttpContext.TraceIdentifier));
            }

            return NoContent();
        }
    }
}