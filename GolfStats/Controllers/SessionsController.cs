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
    public async Task<IActionResult> JoinSession([FromBody] JoinSessionRequest request)
    {
        var sessionId = await GetOrCreateSession();

        var session = await dbContext.Sessions.SingleAsync(x => x.Id == sessionId);

        var player = await dbContext.Players.SingleOrDefaultAsync(x => x.SteamId == request.SteamId)
                     ?? dbContext.Players.Add(new Player
                     {
                         SteamId = request.SteamId,
                         DisplayName = request.DisplayName
                     }).Entity;

        player.DisplayName = request.DisplayName;

        session.Players.Add(player);

        player.CurrentSession = session;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            SessionId = session.Id
        });
    }

    [HttpPost("scores")]
    public async Task<IActionResult> PostScore([FromBody] CreateScoreRequest request)
    {
        var sessionId = await GetOrCreateSession();

        var session = await dbContext.Sessions
            .SingleAsync(x => x.Id == sessionId);

        session.Scores.Add(new ScoreEntry
        {
            Level = request.Level,
            HoleIndex = request.HoleIndex,
            Score = request.Score,
            SteamId = request.SteamId,
            Timestamp = request.Timestamp
        });

        await dbContext.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromBody] LeaveSessionRequest request, CancellationToken cancellationToken)
    {
        var sessionId = await GetOrCreateSession();
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

        session.Players.Remove(player);
        player.CurrentSession = null;

        if (session.Players.Count == 0)
        {
            session.FinishedAt = DateTimeOffset.UtcNow;
            await cache.RemoveAsync(CacheKey, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    private async Task<Guid> GetOrCreateSession()
    {
        Guid sessionId = await cache.GetOrCreateAsync(CacheKey, async ct =>
        {
            EntityEntry<GolfSession> sessionEntry = dbContext.Sessions.Add(new GolfSession
            {
                Id = Guid.CreateVersion7(),
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await dbContext.SaveChangesAsync(ct);

            return sessionEntry.Entity.Id;
        });

        return sessionId;
    }
}
