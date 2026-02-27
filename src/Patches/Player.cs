using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(Player), nameof(Player.InRepairMode))]
    private static class Player_InRepairMode_Patch
    {
        private static void Postfix(Player __instance, ref bool __result)
        {
            Piece selectedPiece = __instance.GetSelectedPiece();
            if (SelectByArea.IsAreaPiece(selectedPiece))
            {
                __result = false;
            }
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    private static class Player_Interact_Patch
    {
        private static bool Prefix(GameObject go) => !go.IsGhost();
    }
    
    public static bool IsGhost(this GameObject go)
    {
        return go.GetComponent<GhostPiece>() ||
               go.GetComponentInParent<GhostPiece>();
    }

    public static bool IsGhost(this Piece piece) => piece != null && piece.gameObject.IsGhost();

    public static bool IsInPlanContainer(this Piece piece, out PlanContainer container)
    {
        container = null;
        if (piece == null) return false;
        container = piece.GetComponentInParent<PlanContainer>(true);
        return container != null;
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.PlacePiece))]
    private static class Player_PlacePiece_Patch
    {
        private static bool Prefix(Player __instance, Piece piece, Vector3 pos, Quaternion rot, bool doAttack)
        {
            if (!__instance.GetRightItem().IsGhostHammer() && 
                !__instance.GetRightItem().IsPlanHammer()) return true;
            
            if (Move.OnPlace(__instance, pos, rot, doAttack))
            {
                return false;
            }
            
            // if (piece.IsConstructionWard() || piece.IsFlagMarker() || piece.IsBlueprintTable()) return true;

            if (ISelectMany.OnPlace(__instance, piece, pos, rot, doAttack))
            {
                return false;
            }

            if (__instance.GetRightItem().IsPlanHammer()) return true;
            GhostPiece.PlacePiece(__instance, piece.gameObject, pos, rot, doAttack);
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
    private static class Player_HaveRequirements_Patch
    {
        private static bool Prefix(Player __instance, Piece piece, Player.RequirementMode mode, ref bool __result)
        {
            switch (mode)
            {
                case Player.RequirementMode.CanBuild 
                    when Move.movingPiece != null && 
                         Move.movingPiece.m_name.Equals(piece.m_name):
                    __result = true;
                    return false;
                case Player.RequirementMode.IsKnown:
                    return true;
            }

            if (!__instance.GetRightItem().IsGhostHammer()) return true;
            
            if (piece.IsConstructionWard()) return true;
            
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetupPlacementGhost))]
    private static class Player_SetupPlacementGhost_Patch
    {
        [HarmonyPriority(Priority.First)]
        private static void Prefix(Player __instance)
        {
            SelectByArea.SetRepairMode(false);
        }
        
        private static void Finalizer(Player __instance)
        {
            SelectByArea.SetRepairMode(true);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    private static class Player_ConsumeResources_Patch
    {
        private static bool Prefix(Player __instance)
        {
            if (!__instance.GetRightItem().IsGhostHammer()) return true;
            Piece selectPiece = __instance.GetSelectedPiece();
            
            if (selectPiece != null && 
                Move.movingPiece != null &&
                Move.movingPiece.m_name.Equals(selectPiece.m_name)) return false;
            
            if (selectPiece.IsConstructionWard() || selectPiece.IsBlueprintTable()) return true;
            return !__instance.InPlaceMode();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetHoverObject))]
    private static class Player_GetHoverObject_Patch
    {
        private static void Postfix(ref GameObject __result)
        {
            if (__result == null || !__result.IsGhost()) return;
            __result = null;
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
    private static class Player_Repair_Patch
    {
        private static bool Prefix(Player __instance, ItemDrop.ItemData toolItem, Piece repairPiece)
        {
            if (!toolItem.IsGhostHammer()) return true;

            if (!ITool.TryGetTool(repairPiece, out ITool tool)) return true;
            tool.OnUse(__instance);
            
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
    private static class Player_UpdatePlacement_Patch
    {
        private static void Prefix(Player __instance)
        {
            if (__instance.m_placementGhost == null ||
                !__instance.m_placementGhost.TryGetComponent(out PlanContainer component)) return;
            
            if (ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKeyDown(KeyCode.LeftShift))
            {
                component.m_piece.m_canRotate = false;
            }
        }

        private static void Postfix(Player __instance)
        {
            if (__instance.m_placementGhost == null ||
                !__instance.m_placementGhost.TryGetComponent(out PlanContainer component)) return;
            component.m_piece.m_canRotate = true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
    private static class Player_UpdatePlacementGhost_Patch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance.m_placementMarkerInstance == null || __instance.m_placementGhost == null ||
                !__instance.m_placementGhost.TryGetComponent(out PlanContainer component)) return;
    
            if (__instance.m_manualSnapPoint > 0) return;
            component.SetPosition(__instance.m_placementMarkerInstance.transform.position);
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateWearNTearHover))]
    private static class Player_UpdateWearNTearHover_Patch
    {
        private static bool Prefix(Player __instance)
        {
            if (!__instance.GetRightItem().IsGhostHammer() || !__instance.InPlaceMode()) return true;
            Select.UpdateHover(__instance);
            
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetBuildPieces))]
    private static class Player_GetBuildPieces_Patch
    {
        private static void Postfix(Player __instance, ref List<Piece> __result)
        {
            if (!Terminal.m_cheat)
            {
                __result.RemoveAll(piece => ITool.m_toolPieces.TryGetValue(piece, out ITool tool) && tool.adminOnly);
            }
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class Player_OnSpawned_Patch
    {
        private static void Postfix()
        {
            foreach (BlueprintRecipe recipe in BlueprintMan.recipes.Values)
            {
                recipe.PostProcess();
            }
        }
    }
}