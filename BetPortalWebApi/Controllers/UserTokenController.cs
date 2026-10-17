using BpDbLayer;
using BpDbLayer.ModelConfigurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BetPortalWebApi.Controllers
{
    [ApiController]
    [Route("api/user-token")]
    public sealed class UserTokenController(BpDbContext dbContext, ILogger<UserTokenController> logger) : ControllerBase
    {
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Activate([FromBody] ActivationRequest request, CancellationToken cancellationToken)
        {
            Response.Headers.CacheControl = "no-store";
            if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 100)
            {
                logger.LogWarning("Account activation rejected an invalid token.");
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid activation token");
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var matches = await dbContext.BpUserTokens.AsNoTracking()
                .Where(token => token.UserInitToken == request.Token)
                .Take(2)
                .ToListAsync(cancellationToken);
            if (matches.Count != 1 || !string.Equals(matches[0].UserInitToken, request.Token, StringComparison.Ordinal))
            {
                logger.LogWarning("Account activation rejected an unknown or ambiguous token.");
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Activation token is invalid or already used");
            }

            var userId = matches[0].UserId;

            // Deleting first locks the token until commit so concurrent requests cannot activate twice.
            var consumed = await dbContext.BpUserTokens
                .Where(token => token.UserId == userId && token.UserInitToken == request.Token)
                .ExecuteDeleteAsync(cancellationToken);
            if (consumed != 1)
            {
                logger.LogWarning("Account activation rejected an already consumed token.");
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "Activation token is invalid or already used");
            }

            var updated = await dbContext.BpUsers.Where(user => user.Id == userId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(user => user.IsActive, true)
                        .SetProperty(user => user.ModifiedDate, DateTime.UtcNow)
                        .SetProperty(user => user.ModifiedByUserId, BpUserConfiguration.AdminUserId),
                    cancellationToken);
            if (updated != 1)
            {
                logger.LogError("Account activation could not update the user associated with the token.");
                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Account activation failed");
            }

            await transaction.CommitAsync(cancellationToken);
            return NoContent();
        }

        public sealed class ActivationRequest
        {
            public string? Token { get; set; }
        }
    }
}
