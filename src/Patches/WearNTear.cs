using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.HaveSupport))]
    private static class WearNTear_HaveSupport_Patch
    {
        private static void Postfix(WearNTear __instance, ref bool __result)
        {
            __result |= __instance.GetComponent<GhostPiece>() || ConstructionWard.IsBuilding();
        }
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Highlight))]
    private static class WearNTear_Highlight_Patch
    {
        private static bool Prefix(WearNTear __instance)
        {
            if (__instance.GetComponent<GhostPiece>() && 
                __instance.TryGetComponent(out Selectable selectable) && 
                ISelectMany.tempPiece == null)
            {
                selectable.Highlight(ConfigManager.HighlightColor);
                return false;
            }
            return true;
        }
    }

    private static bool isRemoving;

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.RPC_Remove))]
    private static class WearNTear_RPC_Remove_Patch
    {
        private static void Prefix()
        {
            isRemoving = true;
        }

        private static void Postfix()
        {
            isRemoving = false;
        }
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy), typeof(HitData),typeof(bool))]
    private static class WearNTear_Destroy_Patch
    {
        private static bool Prefix(WearNTear __instance, HitData hitData, bool blockDrop)
        {
            if (isRemoving) return true;
            
            if (!__instance.m_piece || __instance.m_piece.GetCreator() == 0L) return true;
            if (__instance.GetComponent<GhostPiece>()) return true;
            ConvertToGhost(__instance, __instance.m_piece, hitData, blockDrop);
            return false;
        }

        private static void ConvertToGhost(WearNTear wnt, Piece piece, HitData hitData = null, bool blockDrop = false)
        {
            if (wnt.TryGetComponent(out Bed bed) && wnt.m_nview.IsOwner() && Game.instance != null)
            {
                Game.instance.RemoveCustomSpawnPoint(bed.GetSpawnPoint());
            }
            wnt.m_nview.GetZDO().Set(ZDOVars.s_health, wnt.m_health);
            wnt.m_nview.GetZDO().Set(ZDOVars.s_support, 0.0f);
            wnt.m_onDestroyed?.Invoke();
            wnt.ClearCachedSupport();
            if (wnt.m_destroyNoise > 0.0 && (hitData == null || hitData.m_hitType != HitData.HitType.CinderFire))
            {
                Player closestPlayer = Player.GetClosestPlayer(wnt.transform.position, 10f);
                if (closestPlayer != null) closestPlayer.AddNoise(wnt.m_destroyNoise);
            }
            wnt.m_destroyedEffect.Create(wnt.transform.position, wnt.transform.rotation,
                wnt.transform);
            if (wnt.m_autoCreateFragments)
            {
                wnt.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_CreateFragments");
            }
            if (!blockDrop) piece.DropResources();
            if (wnt.TryGetComponent(out CraftingStation station)) CraftingStation.m_allStations.Remove(station);
            wnt.gameObject.AddComponent<GhostPiece>();
        }
    }
}