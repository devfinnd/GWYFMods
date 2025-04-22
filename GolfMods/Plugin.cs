using System;
using System.Net.Http;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using GolfMods.Powerups;
using GolfMods.StatTracking;
using HarmonyLib;

namespace GolfMods;

[BepInPlugin(GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private Harmony _harmony;
    private const string GUID = $"FinnD.{MyPluginInfo.PLUGIN_GUID}";

    internal new static ManualLogSource Log;

    public static ConfigEntry<float> PuddleLifeSpan { get; private set; }
    public static ConfigEntry<string> ApiUrl { get; private set; }


    public override void Load()
    {
        Log = base.Log;

        _harmony = new Harmony(GUID);
        _harmony.PatchAll(typeof(HoneyPatches));
        _harmony.PatchAll(typeof(StatTrackingPatches));

        PuddleLifeSpan = Config.Bind(
            "Powerups",
            "HoneyLifeSpan",
            float.PositiveInfinity,
            "The lifespan of honey puddles.\nIn-game's default value is 20."
        );

        ApiUrl = Config.Bind(
            "Stats",
            "ApiUrl",
            "http://localhost:5160",
            "The Url to the GWYF Stats server"
        );

        Log.LogInfo($"Plugin {GUID} is loaded!");
    }
}
