using HarmonyLib;
using UnityEngine;

namespace Workshop;

public static partial class Patches
{
    private static float defaultCameraMaxDistance;
    
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.Awake))]
    private static class GameCamera_Awake_Patch
    {
        private static void Postfix(GameCamera __instance)
        {
            defaultCameraMaxDistance = __instance.m_maxDistance;
        }
    }

    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    private static class GameCamera_UpdateCamera_Patch
    {
        private static void Postfix(GameCamera __instance, float dt)
        {
            if (!Player.m_localPlayer) return;
            
            bool updateBuildCamera = !Player.m_localPlayer.IsDead() &&
                                     !Player.m_localPlayer.InCutscene() &&
                                     Player.m_localPlayer.InPlaceMode() &&
                                     Player.m_localPlayer.GetRightItem().IsPlanHammer() &&
                                     Player.m_localPlayer.m_placementGhost != null && 
                                     Player.m_localPlayer.m_placementGhost.GetComponent<PlanContainer>();

            if (!updateBuildCamera)
            {
                __instance.m_maxDistance = Player.m_localPlayer.GetControlledShip() != null ? 
                    __instance.m_maxDistanceBoat : 
                    defaultCameraMaxDistance;
            }
            else
            {
                __instance.m_maxDistance = ConfigManager.MaxCameraDistance;

                if (!ZInput.GetKey(KeyCode.LeftShift) && 
                    !ZInput.GetKeyDown(KeyCode.LeftShift)) return;
                
                float scroll = Mathf.Clamp(ZInput.GetMouseScrollWheel(), -0.05f, 0.05f);

                if (scroll == 0.0f) return;
                __instance.m_distance -= scroll * __instance.m_zoomSens;
                if (ZInput.GetButton("JoyAltKeys") && !Hud.InRadial())
                {
                    if (ZInput.GetButton("JoyCamZoomIn"))
                    {
                        __instance.m_distance -= __instance.m_zoomSens * dt;
                    }
                    else if (ZInput.GetButton("JoyCamZoomOut"))
                    {
                        __instance.m_distance += __instance.m_zoomSens * dt;
                    }
                }

                __instance.m_distance =
                    Mathf.Clamp(
                        __instance.m_distance, 
                        __instance.m_minDistance, 
                        __instance.m_maxDistance);
            }
        }
    }
}