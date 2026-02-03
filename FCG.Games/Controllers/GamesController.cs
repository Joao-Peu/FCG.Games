using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FCG.Games.Models;
using FCG.Games.Services;

namespace FCG.Games.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IGameService _games;
    private readonly IPurchaseService _purchases;

    public GamesController(IGameService games, IPurchaseService purchases)
    {
        _games = games;
        _purchases = purchases;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(Game input)
    {
        var created = await _games.CreateAsync(input);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var g = await _games.GetByIdAsync(id);
        if (g == null) return NotFound();
        return Ok(g);
    }

    [HttpGet("search")]
    [Authorize]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] string? genre, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (items,total) = await _games.SearchAsync(query, genre, minPrice, maxPrice, Math.Max(1,page), Math.Clamp(pageSize,1,100));
        return Ok(new { total, items });
    }

    [HttpPost("{gameId}/purchase")]
    [Authorize]
    public async Task<IActionResult> Purchase(Guid gameId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId)) return Forbid();
        var correlation = Request.Headers["x-correlation-id"].ToString();
        var order = await _purchases.CreatePurchaseIntentAsync(userId, gameId, correlation);
        return Accepted(new { order.Id, order.Status });
    }

    [HttpGet("recommendations")]
    [Authorize]
    public async Task<IActionResult> Recommendations()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        Guid? userId = null;
        if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var uid)) userId = uid;
        var recs = await _games.GetRecommendationsAsync(userId);
        return Ok(recs);
    }
}
