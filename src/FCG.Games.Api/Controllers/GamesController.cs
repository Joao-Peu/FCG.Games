using System.Security.Claims;
using FCG.Games.Application.Abstractions;
using FCG.Games.Application.Commands;
using FCG.Games.Application.DTOs;
using FCG.Games.Application.Queries;
using FCG.Games.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCG.Games.Api.Controllers;

[ApiController]
[Route("api/games")]
[Authorize]
public class GamesController : ControllerBase
{
    private readonly IQueryHandler<ListGamesQuery, Result<IReadOnlyList<GameDto>>> _listGamesHandler;
    private readonly ICommandHandler<PlaceOrderCommand, Result<PurchaseResultDto>> _placeOrderHandler;
    private readonly IQueryHandler<GetRecommendationsQuery, Result<IReadOnlyList<GameDto>>> _recommendationsHandler;

    public GamesController(
        IQueryHandler<ListGamesQuery, Result<IReadOnlyList<GameDto>>> listGamesHandler,
        ICommandHandler<PlaceOrderCommand, Result<PurchaseResultDto>> placeOrderHandler,
        IQueryHandler<GetRecommendationsQuery, Result<IReadOnlyList<GameDto>>> recommendationsHandler)
    {
        _listGamesHandler = listGamesHandler;
        _placeOrderHandler = placeOrderHandler;
        _recommendationsHandler = recommendationsHandler;
    }

    [HttpGet]
    public async Task<IActionResult> ListGames(CancellationToken cancellationToken)
    {
        var result = await _listGamesHandler.HandleAsync(new ListGamesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("{gameId:guid}/purchase")]
    public async Task<IActionResult> Purchase(Guid gameId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var correlationId = Request.Headers["x-correlation-id"].FirstOrDefault();
        var command = new PlaceOrderCommand(userId.Value, gameId, correlationId);
        var result = await _placeOrderHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == Errors.GameNotFound)
                return NotFound(new { error = result.Error.Message });
            return Conflict(new { error = result.Error.Message });
        }

        return Accepted(result.Value);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var query = new GetRecommendationsQuery(userId);
        var result = await _recommendationsHandler.HandleAsync(query, cancellationToken);
        return Ok(result.Value);
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }
}
