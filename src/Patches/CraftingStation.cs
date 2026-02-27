using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    /// <summary>
    /// Prevents hammer from having crafting station included when checking build requirements
    /// </summary>
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
    private static class CraftingStation_Start_Patch
    {
        private static void Postfix(CraftingStation __instance)
        {
            if (__instance.GetComponent<GhostPiece>())
            {
                CraftingStation.m_allStations.Remove(__instance);
            }
        }
    }
}