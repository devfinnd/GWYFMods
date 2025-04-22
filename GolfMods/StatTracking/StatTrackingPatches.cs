using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using ExitGames.Client.Photon;
using HarmonyLib;
using Team17.RealtimeMultiplayer;
using Team17.RealtimeMultiplayer.Steam;
using UnityEngine.SceneManagement;

namespace GolfMods.StatTracking;

[HarmonyPatch]
public static class StatTrackingPatches
{
    private static string _steamId;
    private static string _sessionId;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SessionCoordinator), nameof(SessionCoordinator.Create))]
    private static void PrefixOnCreate(SessionCoordinator __instance)
    {
        try
        {
            Plugin.Log.LogMessage("Creating session...");

            LocalPlayerId localPlayerId = __instance.m_localPlayerId;
            _steamId = localPlayerId.PlatformIdentifier?.SteamId.ToString();

            HttpResponseMessage response = StatsHttpClient.Instance.JoinSession(_steamId, localPlayerId.DisplayName);

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError(
                    $"Failed to create session: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            _sessionId = response.Content.ReadFromJsonAsync<JoinSessionResponse>().Result.SessionId;
            Plugin.Log.LogMessage($"Session {_sessionId} created.");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to create session: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SessionCoordinator), nameof(SessionCoordinator.Join), typeof(RoomInfo), typeof(Hashtable), typeof(SessionJoinCallback))]
    private static void PrefixOnJoinA(SessionCoordinator __instance)
    {
        try
        {
            Plugin.Log.LogMessage("Joining session...");
            LocalPlayerId localPlayerId = __instance.m_localPlayerId;

            _steamId = localPlayerId.PlatformIdentifier?.SteamId.ToString();

            HttpResponseMessage response = StatsHttpClient.Instance.JoinSession(_steamId, localPlayerId.DisplayName);

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError(
                    $"Failed to join session: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            _sessionId = response.Content.ReadFromJsonAsync<JoinSessionResponse>().Result.SessionId;

            Plugin.Log.LogMessage($"Session {_sessionId} joined.");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to join session: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FrontendFlow), nameof(FrontendFlow.OnStartGameSelected))]
    private static void PrefixOnStart()
    {
        if (_sessionId is null) return;

        try
        {
            Plugin.Log.LogMessage("Notifying game started...");
            HttpResponseMessage response = StatsHttpClient.Instance.StartGame(_sessionId);

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError($"Failed to notify game start: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            Plugin.Log.LogMessage("Game start notification sent.");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to notify game start: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BallMovement), nameof(BallMovement.HoleCompleted))]
    private static void PrefixOnHoleCompleted(BallMovement __instance)
    {
        if (_sessionId is null) return;
        if (!__instance.Player.IsLocal) return;

        try
        {
            Plugin.Log.LogMessage("Posting score...");
            Scene activeScene = SceneManager.GetActiveScene();

            HttpResponseMessage response = StatsHttpClient.Instance.PostScore(_sessionId, _steamId, activeScene.name, __instance.HoleNumber.AsHoleIndex().m_Value, __instance.Player.HitCounter);

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError(
                    $"Failed to post score: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            Plugin.Log.LogMessage("Score posted.");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to post score: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InGameMenuFlow), nameof(InGameMenuFlow.OnRetireSelected))]
    private static void PrefixOnOnRetireSelected(InGameMenuFlow __instance)
    {
        if (_sessionId is null) return;

        try
        {
            Plugin.Log.LogMessage("Retired from game, posting stats...");
            Scene activeScene = SceneManager.GetActiveScene();

            HttpResponseMessage response = StatsHttpClient.Instance.EndGame(_sessionId, _steamId, activeScene.name, __instance.m_primaryUser.m_holeScores.Sum());

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError($"Failed to post end game stats: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            Plugin.Log.LogMessage("Stats posted.");

            Plugin.Log.LogMessage("Leaving session...");

            response = StatsHttpClient.Instance.LeaveSession(_sessionId, _steamId);

            _sessionId = null;

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError($"Failed to leave session: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            Plugin.Log.LogMessage("Session left.");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to post end game stats: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BallMovement), nameof(BallMovement.OnEnd))]
    private static void PrefixOnOnEnd(BallMovement __instance)
    {
        if (_sessionId is null) return;

        try
        {
            Plugin.Log.LogMessage("Game ended, posting stats...");
            Scene activeScene = SceneManager.GetActiveScene();

            HttpResponseMessage response = StatsHttpClient.Instance.EndGame(_sessionId, _steamId, activeScene.name, __instance.Player.m_holeScores.Sum());

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError(
                    $"Failed to post end game stats: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            Plugin.Log.LogMessage("End game stats posted.");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to post end game stats: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FrontendFlow), nameof(FrontendFlow.OnLeaveSessionSelected))]
    [HarmonyPatch(typeof(GameFlowController), nameof(GameFlowController.OnApplicationQuit))]
    private static void PrefixOnApplicationQuit()
    {
        if (_sessionId is null) return;

        try
        {
            Plugin.Log.LogMessage($"Leaving session {_sessionId}...");

            HttpResponseMessage response = StatsHttpClient.Instance.LeaveSession(_sessionId, _steamId);

            _sessionId = null;

            if (!response.IsSuccessStatusCode)
            {
                Plugin.Log.LogError(
                    $"Failed to leave session: {response.StatusCode} {response.ReasonPhrase} {response.Content.ReadAsStringAsync().Result}");
                return;
            }

            Plugin.Log.LogMessage("Session left.");
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Failed to leave session: {e}");
        }
    }
}
