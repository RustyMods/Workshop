using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(TextInput), nameof(TextInput.Hide))]
    private static class TextInput_Hide_Patch
    {
        private static void Prefix(TextInput __instance)
        {
            if (__instance.m_queuedSign is OnHideTextReceiver multi)
            {
                multi.OnHide();
            }
        }
    }
}