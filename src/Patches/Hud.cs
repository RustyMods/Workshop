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
        private static bool Prefix(Hud __instance, Piece piece)
        {
            if (!Player.m_localPlayer || piece == null) return true;
            if (!Player.m_localPlayer.GetRightItem().IsGhostHammer()) return true;
            if (piece.m_repairPiece || piece.m_removePiece) return true;
            if (ITool.IsTool(piece) || IPaint.IsPaintTool(piece)) return true;

            DisablePieceRequirements(__instance, piece);
            
            return false;
        }

        private static void DisablePieceRequirements(Hud hud, Piece piece)
        {
            for (int i = 0; i < hud.m_requirementItems.Length; ++i)
            {
                GameObject requirementItem = hud.m_requirementItems[i];
                requirementItem.SetActive(false);
            }
            
            hud.m_buildSelection.text = Localization.instance.Localize(piece.m_name);
            hud.m_pieceDescription.text = Localization.instance.Localize($"{piece.m_description}\n<color=red>GHOST PIECE</color>");
            hud.m_buildIcon.enabled = true;
            hud.m_buildIcon.sprite = piece.m_icon;
            Sprite snappingIconForPiece = hud.GetSnappingIconForPiece(piece);
            hud.m_snappingIcon.sprite = snappingIconForPiece;
            hud.m_snappingIcon.enabled = snappingIconForPiece != null;
        }
    }
}