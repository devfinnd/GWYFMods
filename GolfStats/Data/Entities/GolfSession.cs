namespace GolfStats.Data.Entities;

public sealed record GolfSession
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public ICollection<Player> Players { get; init; } = [];
    public ICollection<ScoreEntry> Scores { get; init; } = [];
}
