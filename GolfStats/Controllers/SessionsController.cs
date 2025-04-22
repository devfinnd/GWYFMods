using GolfStats.Data;
using GolfStats.Data.Entities;
using GolfStats.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Caching.Hybrid;

namespace GolfStats.Controllers;

[ApiController]
[Route("sessions")]
public class SessionsController(HybridCache cache, GolfStatsDbContext dbContext) : ControllerBase
{
    private const string CacheKey = "CurrentSession";

    [HttpPost("join")]
    public async Task<IActionResult> JoinSession([FromBody] JoinSessionRequest request, CancellationToken cancellationToken)
    {
        var sessionId = await GetOrCreateSession(cancellationToken);
        var player = await GetOrCreatePlayer(request.SteamId, request.DisplayName, cancellationToken);

        player.CurrentSessionId = sessionId;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            SessionId = sessionId
        });
    }

    [HttpPost("{sessionId:guid}/start")]
    public async Task<IActionResult> StartGame([FromRoute] Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions
            .SingleAsync(x => x.Id == sessionId, cancellationToken);

        session.StartedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("{sessionId:guid}/scores")]
    public async Task<IActionResult> PostScore([FromRoute] Guid sessionId, [FromBody] CreateScoreRequest request)
    {
        dbContext.Scores.Add(new ScoreEntry
        {
            SessionId = sessionId,
            Level = request.Level,
            HoleIndex = request.HoleIndex,
            Score = request.Score,
            SteamId = request.SteamId,
            Timestamp = request.Timestamp
        });

        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{sessionId:guid}/end")]
    public async Task<IActionResult> End([FromRoute] Guid sessionId, [FromBody] EndGameRequest request, CancellationToken cancellationToken)
    {
        dbContext.Scores.Add(new ScoreEntry
        {
            SessionId = sessionId,
            Level = request.Level,
            HoleIndex = int.MaxValue,
            Score = request.TotalScore,
            SteamId = request.SteamId,
            Timestamp = request.Timestamp,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    [HttpPost("{sessionId:guid}/leave")]
    public async Task<IActionResult> Leave([FromRoute] Guid sessionId, [FromBody] LeaveSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await dbContext.Sessions
            .Include(x => x.Players)
            .SingleAsync(x => x.Id == sessionId, cancellationToken);

        var player = session.Players.FirstOrDefault(x => x.SteamId == request.SteamId);

        if (player is null)
        {
            return NotFound(new ProblemDetails
            {
                Detail = "Player not found in session.",
            });
        }

        player.CurrentSession = null;

        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Entry(session).ReloadAsync(cancellationToken);

        if (session.Players.Count == 0)
        {
            await Task.WhenAll(
             dbContext.Sessions
                .Where(x => x.Id == sessionId)
                .ExecuteUpdateAsync(spc => spc.SetProperty(s => s.FinishedAt, DateTimeOffset.UtcNow), cancellationToken),
             cache.RemoveByTagAsync(sessionId.ToString(), cancellationToken).AsTask());
        }

        return Ok();
    }

    private async Task<Player> GetOrCreatePlayer(string steamId, string displayName, CancellationToken cancellationToken)
    {
        var player = await dbContext.Players.SingleOrDefaultAsync(x => x.SteamId == steamId, cancellationToken) ?? dbContext.Players.Add(new Player
        {
            SteamId = steamId,
            DisplayName = displayName
        }).Entity;

        player.DisplayName = displayName;

        await dbContext.SaveChangesAsync(cancellationToken);

        return player;
    }

    private async Task<Guid> GetOrCreateSession(CancellationToken cancellationToken)
    {
        Guid tempId = Guid.CreateVersion7();
        Guid sessionId = await cache.GetOrCreateAsync(CacheKey, async ct =>
        {
            EntityEntry<GolfSession> sessionEntry = dbContext.Sessions.Add(new GolfSession
            {
                Id = tempId,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await dbContext.SaveChangesAsync(ct);

            return sessionEntry.Entity.Id;
        }, new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromHours(1),
        }, tags: [tempId.ToString()], cancellationToken);

        return sessionId;
    }
}
