namespace GolfStats.Api.Model;

public sealed record LeaveSessionRequest
{
    public required string SteamId { get; init; }
}