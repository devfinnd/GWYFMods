using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace EternalHoney;


[BepInPlugin(GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    private Harmony _harmony;
    private const string GUID = $"FinnD.{MyPluginInfo.PLUGIN_GUID}";

    public static ConfigEntry<float> PuddleLifeSpan { get; private set; }


    public override void Load()
    {
        _harmony = new Harmony(GUID);
        _harmony.PatchAll(typeof(HoneyPatches));

        PuddleLifeSpan = Config.Bind(
            "Powerups",
            "HoneyLifeSpan",
            float.PositiveInfinity,
            "The lifespan of honey puddles.\nIn-game's default value is 20."
        );

        Log.LogInfo($"Plugin {GUID} is loaded!");
    }
}

[HarmonyPatch(typeof(PowerupStickyGlue))]
public static class HoneyPatches
{
    [HarmonyPatch(nameof(PowerupStickyGlue.Activate))]
    [HarmonyPrefix]
    private static void ActivatePrefix(PowerupStickyGlue __instance)
    {
        __instance.m_puddleLifeSpanSeconds = Plugin.PuddleLifeSpan.Value;
    }
}
