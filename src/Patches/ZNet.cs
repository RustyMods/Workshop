using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    private static class ZNet_Awake_Patch
    {
        private static void Postfix(ZNet __instance)
        {
            if (__instance.IsServer())
            {
                Marketplace.Init();
            }
            else
            {
                Marketplace.sync.ValueChanged += Marketplace.OnBlueprintSoldChanged;
            }

            BlueprintMan.OnZNetAwake(__instance);
        }
    }
}