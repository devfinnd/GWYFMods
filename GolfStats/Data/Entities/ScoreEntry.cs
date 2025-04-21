namespace GolfStats.Data.Entities;

public sealed record ScoreEntry
{
    public Guid SessionId { get; set; }
    public string SteamId { get; set; }
    public string Level { get; set; }
    public int HoleIndex { get; set; }
    public int Score { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public GolfSession Session { get; set; }
    public Player Player { get; set; }
}
