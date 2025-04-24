using System;
using System.Linq;
using System.Net.Http;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using ExitGames.Client.Photon;
using HarmonyLib;
using Team17.RealtimeMultiplayer;
using Team17.RealtimeMultiplayer.Steam;
using UnityEngine.SceneManagement;

namespace GolfTracker;

[BepInPlugin(GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private Harmony _harmony;
    private const string GUID = $"FinnD.{MyPluginInfo.PLUGIN_GUID}";

    public static ManualLogSource Logger { get; private set; }
    public static Tracker Tracker { get; private set; }


    public override void Load()
    {
        Logger = Log;

        _harmony = new Harmony(GUID);
        _harmony.PatchAll(typeof(StatTrackingPatches));

        ConfigEntry<string> apiUrl = Config.Bind(
            "Stats",
            "ApiUrl",
            "http://localhost:5160",
            "The Url to the GWYF Stats server"
        );

        if (string.IsNullOrWhiteSpace(apiUrl.Value))
        {
            Logger.LogError("API URL is empty. Please set a valid URL in the config file.");
            return;
        }

        if (!Uri.TryCreate(apiUrl.Value, UriKind.Absolute, out Uri baseUrl))
        {
            Logger.LogError($"Invalid API URL: {apiUrl.Value}");
            return;
        }

        Tracker = new Tracker(new StatsHttpClient(new HttpClient
        {
            BaseAddress = baseUrl
        }));

        Logger.LogInfo($"Plugin {GUID} is loaded!");
    }
}

[HarmonyPatch]
public static class StatTrackingPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(SessionCoordinator), nameof(SessionCoordinator.Create))]
    private static void PrefixOnCreate(SessionCoordinator __instance)
    {
        try
        {
            _ = Plugin.Tracker.CreateSession(
                __instance.m_localPlayerId.PlatformIdentifier?.SteamId.ToString(),
                __instance.m_localPlayerId.DisplayName);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Failed to create session: {e}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SessionCoordinator), nameof(SessionCoordinator.Join), typeof(RoomInfo), typeof(Hashtable), typeof(SessionJoinCallback))]
    private static void PrefixOnJoinA(SessionCoordinator __instance)
    {
        _ = Plugin.Tracker.JoinSession(
            __instance.m_localPlayerId.PlatformIdentifier.SteamId.ToString(),
            __instance.m_localPlayerId.DisplayName);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FrontendFlow), nameof(FrontendFlow.OnStartGameSelected))]
    private static void PrefixOnStart()
    {
        _ = Plugin.Tracker.StartGame();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BallMovement), nameof(BallMovement.HoleCompleted))]
    private static void PrefixOnHoleCompleted(BallMovement __instance)
    {
        _ = Plugin.Tracker.HoleCompleted(
            SceneManager.GetActiveScene().name,
            __instance.HoleNumber.AsHoleIndex().m_Value,
            __instance.Player.HitCounter);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InGameMenuFlow), nameof(InGameMenuFlow.OnRetireSelected))]
    private static void PrefixOnOnRetireSelected(InGameMenuFlow __instance)
    {
        _ = Plugin.Tracker.Retire(
            SceneManager.GetActiveScene().name,
            __instance.m_primaryUser.m_holeScores.Sum());
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BallMovement), nameof(BallMovement.OnEnd))]
    private static void PrefixOnOnEnd(BallMovement __instance)
    {
        _ = Plugin.Tracker.EndGame(
            SceneManager.GetActiveScene().name,
            __instance.Player.HitCounter);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FrontendFlow), nameof(FrontendFlow.OnLeaveSessionSelected))]
    [HarmonyPatch(typeof(GameFlowController), nameof(GameFlowController.OnApplicationQuit))]
    private static void PrefixOnApplicationQuit()
    {
        _ = Plugin.Tracker.Quit();
    }
}
