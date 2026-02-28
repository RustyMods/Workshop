using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    private static class Game_Logout_Patch
    {
        private static void Prefix()
        {
            BlueprintMan.Dispose();
            Marketplace.OnLogout();
        }
    }
}