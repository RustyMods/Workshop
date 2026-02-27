using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(StationExtension), nameof(StationExtension.Awake))]
    private static class StationExtension_Awake_Patch
    {
        private static void Postfix(StationExtension __instance)
        {
            if (!__instance.GetComponent<GhostPiece>()) return;
            StationExtension.m_allExtensions.Remove(__instance);
            __instance.CancelInvoke(nameof(StationExtension.UpdateConnection));
            __instance.StopConnectionEffect();
        }
    }
}