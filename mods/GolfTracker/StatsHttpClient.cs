using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace GolfTracker;

public sealed class StatsHttpClient
{
    private readonly HttpClient _innerClient;

    public StatsHttpClient(HttpClient innerClient)
    {
        _innerClient = innerClient;
    }

    public Task<HttpResponseMessage> JoinSession(string steamId, string displayName)
    {
        return _innerClient.PostAsJsonAsync("/sessions/join", new
        {
            SteamId = steamId,
            DisplayName = displayName
        });
    }

    public Task<HttpResponseMessage> StartGame(string sessionId)
    {
        return _innerClient.PostAsync($"/sessions/{sessionId}/start", null);
    }

    public Task<HttpResponseMessage> PostScore(string sessionId, string steamId, string level, int holeIndex, int score)
    {
        return _innerClient.PostAsJsonAsync($"/sessions/{sessionId}/scores", new
        {
            SessionId = sessionId,
            SteamId = steamId,
            Level = level,
            HoleIndex = holeIndex,
            Score = score,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<HttpResponseMessage> EndGame(string sessionId, string steamId, string level, int totalScore)
    {
        return _innerClient.PostAsJsonAsync($"/sessions/{sessionId}/end", new
        {
            SessionId = sessionId,
            SteamId = steamId,
            Level = level,
            TotalScore = totalScore,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    public Task<HttpResponseMessage> LeaveSession(string sessionId, string steamId)
    {
        return _innerClient.PostAsJsonAsync($"/sessions/{sessionId}/leave", new
        {
            SessionId = sessionId,
            SteamId = steamId
        });
    }
}
