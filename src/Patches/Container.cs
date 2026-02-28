using HarmonyLib;
using UnityEngine;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    private static class Container_GetHoverText
    {
        private static bool Prefix(Container __instance, ref string __result)
        {
            if (__instance.TryGetComponent(out ConstructionWard ward))
            {
                __result = ward.GetHoverText();
                return false;
            }
            return true;
        }
    }
    
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    private static class Container_Interact_Patch
    {
        private static bool Prefix(Container __instance, Humanoid character, bool hold, bool alt)
        {
            if (__instance.TryGetComponent(out ConstructionWard ward) && 
                character is Player player &&
                (ZInput.GetKey(KeyCode.LeftAlt) || ZInput.GetButton(ConstructionWard.BUILD_KEY)))
            {
                if (ward.IsBuilding()) ward.Cancel();
                else ward.Build(player);
                return false;
            }
    
            return true;
        }
    }
}