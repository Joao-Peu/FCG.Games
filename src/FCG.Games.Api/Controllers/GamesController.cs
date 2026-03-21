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
    private readonly IQueryHandler<GetGameByIdQuery, Result<GameDto>> _getGameByIdHandler;
    private readonly ICommandHandler<CreateGameCommand, Result<GameDto>> _createGameHandler;
    private readonly ICommandHandler<UpdateGameCommand, Result<GameDto>> _updateGameHandler;
    private readonly ICommandHandler<DeleteGameCommand, Result> _deleteGameHandler;
    private readonly ICommandHandler<PlaceOrderCommand, Result<PurchaseResultDto>> _placeOrderHandler;
    private readonly IQueryHandler<GetRecommendationsQuery, Result<IReadOnlyList<GameDto>>> _recommendationsHandler;
    private readonly IQueryHandler<GetUserLibraryQuery, Result<IReadOnlyList<GameDto>>> _userLibraryHandler;

    public GamesController(
        IQueryHandler<ListGamesQuery, Result<IReadOnlyList<GameDto>>> listGamesHandler,
        IQueryHandler<GetGameByIdQuery, Result<GameDto>> getGameByIdHandler,
        ICommandHandler<CreateGameCommand, Result<GameDto>> createGameHandler,
        ICommandHandler<UpdateGameCommand, Result<GameDto>> updateGameHandler,
        ICommandHandler<DeleteGameCommand, Result> deleteGameHandler,
        ICommandHandler<PlaceOrderCommand, Result<PurchaseResultDto>> placeOrderHandler,
        IQueryHandler<GetRecommendationsQuery, Result<IReadOnlyList<GameDto>>> recommendationsHandler,
        IQueryHandler<GetUserLibraryQuery, Result<IReadOnlyList<GameDto>>> userLibraryHandler)
    {
        _listGamesHandler = listGamesHandler;
        _getGameByIdHandler = getGameByIdHandler;
        _createGameHandler = createGameHandler;
        _updateGameHandler = updateGameHandler;
        _deleteGameHandler = deleteGameHandler;
        _placeOrderHandler = placeOrderHandler;
        _recommendationsHandler = recommendationsHandler;
        _userLibraryHandler = userLibraryHandler;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ListGames(CancellationToken cancellationToken)
    {
        var result = await _listGamesHandler.HandleAsync(new ListGamesQuery(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getGameByIdHandler.HandleAsync(new GetGameByIdQuery(id), cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error.Message });
        return Ok(result.Value);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateGameCommand command, CancellationToken cancellationToken)
    {
        var result = await _createGameHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGameCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest(new { error = "Route id does not match body id." });

        var result = await _updateGameHandler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error.Message });
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deleteGameHandler.HandleAsync(new DeleteGameCommand(id), cancellationToken);
        if (result.IsFailure)
            return NotFound(new { error = result.Error.Message });
        return NoContent();
    }

    [HttpPost("{gameId:guid}/purchase")]
    public async Task<IActionResult> Purchase(Guid gameId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var command = new PlaceOrderCommand(userId.Value, gameId);
        var result = await _placeOrderHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error == Errors.GameNotFound)
                return NotFound(new { error = result.Error.Message });
            return Conflict(new { error = result.Error.Message });
        }

        return Accepted(result.Value);
    }

    [HttpGet("library")]
    public async Task<IActionResult> GetUserLibrary(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var query = new GetUserLibraryQuery(userId.Value);
        var result = await _userLibraryHandler.HandleAsync(query, cancellationToken);
        return Ok(result.Value);
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
