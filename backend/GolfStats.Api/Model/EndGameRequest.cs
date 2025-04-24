namespace GolfStats.Api.Model;

public sealed record EndGameRequest
{
    public required string Level { get; init; }
    public required int TotalScore { get; init; }
    public required string SteamId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}