using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GolfTracker.Model;

namespace GolfTracker;

public sealed class Tracker(StatsHttpClient client)
{
    public string SteamId { get; private set; }
    public string SessionId { get; private set; }

    public async Task CreateSession(string steamId, string displayName)
    {
        Plugin.Logger.LogMessage("Creating session...");

        SteamId = steamId;

        HttpResponseMessage result = await client.JoinSession(steamId, displayName);

        if (!result.IsSuccessStatusCode)
        {
            string content = await result.Content.ReadAsStringAsync();
            Plugin.Logger.LogError($"Failed to create session: {result.StatusCode} {result.ReasonPhrase} {content}");
            return;
        }

        JoinSessionResponse response = await result.Content.ReadFromJsonAsync<JoinSessionResponse>();
        SessionId = response.SessionId;

        Plugin.Logger.LogMessage($"Session {SessionId} created.");
    }

    public async Task JoinSession(string steamId, string displayName)
    {
        try
        {
            Plugin.Logger.LogMessage("Joining session...");

            SteamId = steamId;

            HttpResponseMessage result = await client.JoinSession(steamId, displayName);

            if (!result.IsSuccessStatusCode)
            {
                string content = await result.Content.ReadAsStringAsync();
                Plugin.Logger.LogError($"Failed to join session: {result.StatusCode} {result.ReasonPhrase} {content}");
                return;
            }

            JoinSessionResponse response = await result.Content.ReadFromJsonAsync<JoinSessionResponse>();
            SessionId = response.SessionId;

            Plugin.Logger.LogMessage($"Session {SessionId} joined.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Failed to join session: {e}");
        }
    }

    public async Task StartGame()
    {
        try
        {
            Plugin.Logger.LogMessage("Notifying game started...");
            HttpResponseMessage response = await client.StartGame(SessionId);

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Plugin.Logger.LogError($"Failed to notify game start: {response.StatusCode} {response.ReasonPhrase} {content}");
                return;
            }

            Plugin.Logger.LogMessage("Game start notification sent.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Failed to notify game start: {e}");
        }
    }

    public async Task HoleCompleted(string level, int hole, int score)
    {
        try
        {
            Plugin.Logger.LogMessage("Posting score...");

            HttpResponseMessage response = await client.PostScore(SessionId, SteamId, level, hole, score);

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Plugin.Logger.LogError($"Failed to post score: {response.StatusCode} {response.ReasonPhrase} {content}");
                return;
            }

            Plugin.Logger.LogMessage("Score posted.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Failed to post score: {e}");
        }
    }

    public async Task Retire(string level, int totalScore)
    {
        try
        {
            Plugin.Logger.LogMessage("Retired from game, posting stats...");

            HttpResponseMessage response = await client.EndGame(SessionId, SteamId, level, totalScore);

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Plugin.Logger.LogError($"Failed to post end game stats: {response.StatusCode} {response.ReasonPhrase} {content}");
                return;
            }

            Plugin.Logger.LogMessage("Stats posted.");

            Plugin.Logger.LogMessage("Leaving session...");

            response = await client.LeaveSession(SessionId, SteamId);

            SessionId = null;

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Plugin.Logger.LogError($"Failed to leave session: {response.StatusCode} {response.ReasonPhrase} {content}");
                return;
            }

            Plugin.Logger.LogMessage("Session left.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Failed to post end game stats: {e}");
        }
    }

    public async Task EndGame(string level, int totalScore)
    {
        try
        {
            Plugin.Logger.LogMessage("Game ended, posting stats...");

            HttpResponseMessage response = await client.EndGame(SessionId, SteamId, level, totalScore);

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Plugin.Logger.LogError($"Failed to post end game stats: {response.StatusCode} {response.ReasonPhrase} {content}");
                return;
            }

            Plugin.Logger.LogMessage("End game stats posted.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Failed to post end game stats: {e}");
        }
    }

    public async Task Quit()
    {
        try
        {
            Plugin.Logger.LogMessage($"Leaving session {SessionId}...");

            HttpResponseMessage response = await client.LeaveSession(SessionId, SteamId);

            SessionId = null;

            if (!response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                Plugin.Logger.LogError($"Failed to leave session: {response.StatusCode} {response.ReasonPhrase} {content}");
                return;
            }

            Plugin.Logger.LogMessage("Session left.");
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Failed to leave session: {e}");
        }
    }
}
