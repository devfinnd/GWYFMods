using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ExitGames.Client.Photon;
using GolfMods.Powerups;
using HarmonyLib;
using Team17.RealtimeMultiplayer;
using Team17.RealtimeMultiplayer.Steam;
using UnityEngine.SceneManagement;

namespace GolfMods;

[BepInPlugin(GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public const string GUID = $"FinnD.{MyPluginInfo.PLUGIN_GUID}";

    internal new static ManualLogSource Log;
    internal static HttpClient HttpClient;

    public static ConfigEntry<float> PuddleLifeSpan { get; private set; }
    public static ConfigEntry<string> ApiUrl { get; private set; }


    public override void Load()
    {
        Log = base.Log;

        var harmony = new Harmony(GUID);
        harmony.PatchAll(typeof(PowerupStickyGluePatches));
        harmony.PatchAll(typeof(FeedPatch));

        PuddleLifeSpan = Config.Bind(
            "Powerups",
            "PuddleLifeSpan",
            float.PositiveInfinity,
            "The new lifespan of honey puddles.\nIn-game's default value is 20."
        );

        ApiUrl = Config.Bind(
            "Stats",
            "ApiUrl",
            "http://localhost:5160",
            "The Url to the GWYF Stats server"
        );

        HttpClient = new HttpClient()
        {
            BaseAddress = new Uri(ApiUrl.Value)
        };

        Log.LogInfo($"Plugin {GUID} is loaded!");
    }
}

[HarmonyPatch]
public static class FeedPatch
{
    private static string _steamId;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SessionCoordinator), nameof(SessionCoordinator.Create))]
    private static void PrefixOnCreate(SessionCoordinator __instance)
    {
        LocalPlayerId localPlayerId = __instance.m_localPlayerId;
        Plugin.Log.LogMessage("Creating session...");
        _ = Plugin.HttpClient.PostAsJsonAsync("/sessions/join", new
        {
            SteamId = localPlayerId.PlatformIdentifier?.SteamId.ToString(),
            DisplayName = localPlayerId.DisplayName,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SessionCoordinator), nameof(SessionCoordinator.Join), typeof(RoomInfo), typeof(Hashtable), typeof(SessionJoinCallback))]
    private static void PrefixOnJoinA(SessionCoordinator __instance)
    {
        LocalPlayerId localPlayerId = __instance.m_localPlayerId;

        Plugin.Log.LogMessage("Joining session...");
        _ = Plugin.HttpClient.PostAsJsonAsync("/sessions/join", new
        {
            SteamId = localPlayerId.PlatformIdentifier?.SteamId.ToString(),
            DisplayName = localPlayerId.DisplayName,
        }).ContinueWith(x =>
        {
            HttpResponseMessage response = x.Result;
            if (response.IsSuccessStatusCode)
            {
                Plugin.Log.LogMessage($"Player joined session successfully.");
            }
            else
            {
                Plugin.Log.LogError($"Failed to join session: {response.StatusCode}");
            }
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BallMovement), nameof(BallMovement.HoleCompleted))]
    private static void PrefixOnHoleCompleted(BallMovement __instance)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Plugin.Log.LogMessage($"[{activeScene.name}] {__instance.Player.DisplayName} Scored {__instance.Player.HitCounter} on hole {__instance.HoleNumber.m_Value}.");

        if (!__instance.Player.IsLocal) return;

        Plugin.Log.LogMessage("Posting score...");
        _ = Plugin.HttpClient.PostAsJsonAsync("/sessions/scores", new
        {
            Level = activeScene.name,
            HoleIndex = __instance.HoleNumber.AsHoleIndex().m_Value,
            Score = __instance.Player.HitCounter,
            SteamId = __instance.Player.PlatformIDString,
            Timestamp = DateTimeOffset.UtcNow
        }).ContinueWith(x =>
        {
            HttpResponseMessage response = x.Result;
            if (response.IsSuccessStatusCode)
            {
                Plugin.Log.LogMessage($"[{activeScene.name}] Score posted successfully.");
            }
            else
            {
                Plugin.Log.LogError($"[{activeScene.name}] Failed to post score: {response.StatusCode}");
            }
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameFlowController), nameof(GameFlowController.UpdateStats_GameEnd))]
    private static void PrefixOnUpdateStats_GameEnd(GameFlowController __instance)
    {
        Plugin.Log.LogMessage("Game ended, posting stats...");
        _ = Plugin.HttpClient.PostAsJsonAsync("/session/leave", new
        {
            SteamId = _steamId
        }).ContinueWith(x =>
        {
            HttpResponseMessage response = x.Result;
            if (response.IsSuccessStatusCode)
            {
                Plugin.Log.LogMessage($"Player left session successfully.");
            }
            else
            {
                Plugin.Log.LogError($"Failed to leave session: {response.StatusCode}");
            }
        });
    }
}
