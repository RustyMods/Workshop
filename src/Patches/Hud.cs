using HarmonyLib;
using UnityEngine;

namespace Workshop;

public static partial class Patches
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateBuild))]
    private static class Hud_UpdateBuild_Patch
    {
        private static void Postfix(Hud __instance, Player player)
        {
            //TODO: figure out a better place to do this rather than update
            if (player.InPlaceMode() && !__instance.m_pieceSelectionWindow.activeSelf) return;
            Select.hovering = null;
            ISelectMany.tempPiece = null;
            Move.movingPiece = null;
        }
    }

    [HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo))]
    private static class Hud_SetupPieceInfo_Patch
    {
        private static void Postfix(Hud __instance, Piece piece)
        {
            if (piece == null) return;
            
            if (IPaint.IsPaintTool(piece) && PaintOptions.instance != null)
            {
                __instance.m_pieceDescription.text = PaintOptions.instance.m_pieceInfo;
            }
            else if (Player.m_localPlayer && 
                     Player.m_localPlayer.GetRightItem().IsGhostHammer() && 
                     !piece.m_repairPiece && 
                     !piece.m_removePiece &&
                     !ITool.IsTool(piece) &&
                     !IPaint.IsPaintTool(piece))
            {
                __instance.m_buildSelection.text += " ( <color=red>GHOST</color> )";
            }
        }
    }
}