using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    private static class Piece_DropResources_Patch
    {
        private static bool Prefix(Piece __instance) => !__instance.IsGhost();
    }
}