using HarmonyLib;

namespace Workshop;

public static partial class Patches
{
    private static bool IsSelectToolPiece(Piece piece) => ISelectMany.SelectTools.Contains(piece);
    private static bool IsRemoveToolPiece(Piece piece) => ISelectMany.RemoveTools.Contains(piece);
    
    [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.GetSelectedPiece))]
    private static class PieceTable_GetSelectedPiece_Patch
    {
        private static void Postfix(ref Piece __result)
        {
            if (__result == null || !Player.m_localPlayer) return;
            if (!Player.m_localPlayer.GetRightItem().IsGhostHammer()) return;

            if (!IsSelectToolPiece(__result) || IsRemoveToolPiece(__result))
            {
                ISelectMany.tempPiece = null;
            }
            
            if (ISelectMany.tempPiece != null)
            {
                __result = ISelectMany.tempPiece;
            }
        }
    }    
}