using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake))]
    private static class ZNetView_Awake_Patch
    {
        private static void Postfix(ZNetView __instance)
        {
            if (__instance.GetZDO() == null) return;
            bool isGhost = __instance.GetZDO().GetBool(GhostVars.IsGhost);
            if (isGhost)
            {
                __instance.gameObject.AddComponent<GhostPiece>();
            }
        }
    }
}