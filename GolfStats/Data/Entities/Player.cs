namespace GolfStats.Data.Entities;

public sealed record Player
{
    public string SteamId { get; set; }
    public string DisplayName { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public GolfSession? CurrentSession { get; set; }
    public ICollection<ScoreEntry> Scores { get; init; } = [];
}
