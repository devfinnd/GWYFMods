namespace GolfStats.Api.Model;

public sealed record JoinSessionRequest
{
    public required string SteamId { get; init; }
    public required string DisplayName { get; init; }
}