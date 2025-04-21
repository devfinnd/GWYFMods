namespace GolfStats.Model;

public sealed record CreateScoreRequest
{
    public required string Level { get; init; }
    public required int HoleIndex { get; init; }
    public required int Score { get; init; }
    public required string SteamId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
