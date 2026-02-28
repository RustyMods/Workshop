using System.Collections.Generic;
using System.Linq;
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
            if (!ConfigManager.ShowAllPieces && __instance.GetRightItem().IsGhostHammer())
            {
                __result.RemoveAll(GhostHammer.tool.IsUnknownPiece);
            }
            if (Terminal.m_cheat) return;
            __result.RemoveAll(IsAdminPiece);
        }
    }

    private static bool IsAdminPiece(Piece piece) =>
        (ITool.m_toolPieces.TryGetValue(piece, out ITool tool) && tool.adminOnly) ||
        (IPaint.m_paintTools.TryGetValue(piece, out IPaint paint) && paint.adminOnly);
    
    
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class Player_OnSpawned_Patch
    {
        private static void Postfix()
        {
            List<BlueprintRecipe> recipes = BlueprintMan.recipes.Values.ToList();
            List<TempBlueprint> temps = BlueprintMan.temps.Values.ToList();

            for (int i = 0; i < recipes.Count; ++i)
            {
                BlueprintRecipe recipe = recipes[i];
                recipe.PostProcess();
            }

            for (int i = 0; i < temps.Count; ++i)
            {
                TempBlueprint temp = temps[i];
                temp.PostProcess();
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.RemovePiece))]
    private static class Player_RemovePiece_Patch
    {
        private static void Postfix(Player __instance, ref bool __result)
        {
            if (!__instance.GetRightItem().IsGhostHammer()) return;

            if (Select.hovering == null || !Select.hovering.GetComponent<GhostPiece>()) return;
            if (!Select.hovering.TryGetComponent(out ZNetView view)) return;
            view.ClaimOwnership();
            view.Destroy();
            __result = true;
        }
    }

    // [HarmonyPatch(typeof(Character), nameof(Character.UpdateLava))]
    // private static class Character_UpdateLava_Patch
    // {
    //     private static bool Prefix(Character __instance, float dt)
    //     {
    //         if (WorldGenerator.IsAshlands(__instance.transform.position.x, __instance.transform.position.z))
    //             return true;
    //         
    //         __instance.m_lavaTimer += dt;
    //         __instance.m_aboveOrInLavaTimer += dt;
    //         
    //         Vector3 position = __instance.transform.position;
    //         ZoneSystem.instance.GetGroundData(ref position, out Vector3 _, out var biome, out Heightmap.BiomeArea _, out var hmap);
    //         if (hmap == null) return false;
    //         
    //         biome = hmap.GetBiomeFromMesh(position);
    //         if (biome != Heightmap.Biome.AshLands) return false;
    //         
    //         float lava = hmap.GetVegetationMask(position);
    //         __instance.m_lavaProximity = Mathf.Min(1f, Utils.SmoothStep(0.1f, 1f, lava));
    //         
    //         __instance.m_lavaProximity = 0.0f;
    //
    //         if (__instance.m_lavaProximity > __instance.m_minLavaMaskThreshold)
    //         {
    //             __instance.m_aboveOrInLavaTimer = 0.0f;
    //         }
    //         
    //         __instance.m_lavaHeightFactor = __instance.transform.position.y - position.y;
    //         __instance.m_lavaHeightFactor = (__instance.m_lavaAirDamageHeight - __instance.m_lavaHeightFactor) / __instance.m_lavaAirDamageHeight;
    //         
    //         bool flag = false;
    //         
    //         if (__instance.m_lavaProximity > __instance.m_minLavaMaskThreshold &&
    //             Physics.Raycast(__instance.transform.position + Vector3.up, Vector3.down, out var hitInfo, 50f, Character.s_blockedRayMask) &&
    //             hitInfo.collider.GetComponent<Heightmap>() == null)
    //         {
    //             flag = true;
    //         }
    //
    //         if (!flag && __instance.IsRiding())
    //         {
    //             flag = true;
    //         }
    //         float num = 1f - __instance.GetEquipmentHeatResistanceModifier();
    //         
    //         if (Terminal.m_showTests && __instance.IsPlayer())
    //         {
    //             Terminal.m_testList["Lava/Height/Resist"] = $"{__instance.m_lavaProximity:0.00} / {__instance.m_lavaHeightFactor:0.00} / {num:0.00}";
    //         }
    //         
    //         if (__instance.m_lavaProximity >  __instance.m_minLavaMaskThreshold &&  __instance.m_lavaHeightFactor > 0.0 && !flag)
    //         {
    //           __instance.m_lavaHeatLevel += __instance.m_lavaProximity * dt * __instance.m_heatBuildupBase * __instance.m_lavaHeightFactor * num;
    //           __instance.m_lavaTimer = 0.0f;
    //         }
    //         else if (__instance.m_dayHeatGainRunning != 0.0 &&
    //                  __instance.IsPlayer() && EnvMan.IsDay() &&
    //                  !__instance.IsUnderRoof() &&
    //                  __instance.GetEquipmentHeatResistanceModifier() < __instance.m_dayHeatEquipmentStop)
    //         {
    //             if (__instance.m_currentVel.magnitude > 0.10000000149011612 && __instance.IsOnGround())
    //             {
    //                 __instance.m_lavaHeatLevel += dt * __instance.m_dayHeatGainRunning * num;
    //             }
    //             else if (!__instance.InWater())
    //             {
    //                 __instance.m_lavaHeatLevel += dt * __instance.m_dayHeatGainStill;
    //             }
    //
    //             if (__instance.m_lavaHeatLevel > __instance.m_heatLevelFirstDamageThreshold)
    //             {
    //                 __instance.m_lavaHeatLevel = __instance.m_heatLevelFirstDamageThreshold;
    //             }
    //         }
    //         else if (!__instance.InWater())
    //         {
    //             __instance.m_lavaHeatLevel -= dt * __instance.m_heatCooldownBase;
    //         }
    //
    //         __instance.m_lavaHeatLevel = __instance.m_tolerateFire ? 0.0f : Mathf.Clamp(__instance.m_lavaHeatLevel, 0.0f, 1f);
    //
    //         return false;
    //     }
    // }
}