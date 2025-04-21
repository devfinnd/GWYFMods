using HarmonyLib;

namespace GolfMods.Powerups;

[HarmonyPatch(typeof(PowerupStickyGlue))]
public static class PowerupStickyGluePatches
{
    [HarmonyPatch(nameof(PowerupStickyGlue.Activate))]
    [HarmonyPrefix]
    private static void ActivatePrefix(PowerupStickyGlue __instance)
    {
        __instance.m_puddleLifeSpanSeconds = Plugin.PuddleLifeSpan.Value;
    }
}
