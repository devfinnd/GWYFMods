using System;
using System.Net.Http;
using System.Net.Http.Json;

namespace GolfMods.StatTracking;

public record JoinSessionResponse(string SessionId);

public sealed class StatsHttpClient
{
    private static readonly Lazy<StatsHttpClient> _instance;

    static StatsHttpClient()
    {
        _instance = new Lazy<StatsHttpClient>(() =>
        {
            HttpClient client = new()
            {
                BaseAddress = new Uri(Plugin.ApiUrl.Value)
            };

            return new StatsHttpClient(client);
        });
    }

    private readonly HttpClient _innerClient;

    private StatsHttpClient(HttpClient innerClient)
    {
        _innerClient = innerClient;
    }

    public static StatsHttpClient Instance => _instance.Value;

    public HttpResponseMessage JoinSession(string steamId, string displayName)
    {
        var request = new
        {
            SteamId = steamId,
            DisplayName = displayName
        };

        return _innerClient.Send(new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_innerClient.BaseAddress,"/sessions/join"),
            Content = JsonContent.Create(request)
        });
    }

    public HttpResponseMessage StartGame(string sessionId)
    {
        return _innerClient.Send(new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_innerClient.BaseAddress,$"/sessions/{sessionId}/start"),
        });
    }

    public HttpResponseMessage PostScore(string sessionId, string steamId, string level, int holeIndex, int score)
    {
        var request = new
        {
            SessionId = sessionId,
            SteamId = steamId,
            Level = level,
            HoleIndex = holeIndex,
            Score = score,
            Timestamp = DateTimeOffset.UtcNow
        };

        return _innerClient.Send(new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_innerClient.BaseAddress,$"/sessions/{sessionId}/scores"),
            Content = JsonContent.Create(request)
        });
    }

    public HttpResponseMessage EndGame(string sessionId, string steamId, string level, int totalScore)
    {
        var request = new
        {
            SessionId = sessionId,
            SteamId = steamId,
            Level = level,
            TotalScore = totalScore,
            Timestamp = DateTimeOffset.UtcNow
        };

        return _innerClient.Send(new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri =new Uri(_innerClient.BaseAddress,$"/sessions/{sessionId}/end"),
            Content = JsonContent.Create(request)
        });
    }

    public HttpResponseMessage LeaveSession(string sessionId, string steamId)
    {
        var request = new
        {
            SessionId = sessionId,
            SteamId = steamId
        };

        return _innerClient.Send(new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(_innerClient.BaseAddress,$"/sessions/{sessionId}/leave"),
            Content = JsonContent.Create(request)
        });
    }
}
