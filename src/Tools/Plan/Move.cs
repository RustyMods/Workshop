using UnityEngine;

namespace Workshop;

public class Move : ITool
{
    public static Piece movingPiece;
    private static Move Tool;
    
    public Move(string id, string name, int index = 1) : base(id, name, index)
    {
        Tool = this;
        piece.m_icon = SpriteManager.GetSprite("move_icon.png");
    }

    public override void OnUse(Player player)
    {
        Piece hoverPiece = player.GetHoveringPiece();
        if (hoverPiece == null) return;
        player.SetSelectedPiece(hoverPiece);
        movingPiece = hoverPiece;
        player.SetupPlacementGhost();
        Workshop.LogDebug($"Moving {hoverPiece.name}");
    }

    public static bool OnPlace(Player player, Vector3 pos, Quaternion rot, bool doAttack)
    {
        if (movingPiece == null) return false;
        
        movingPiece.transform.position = pos;
        movingPiece.transform.rotation = rot;

        ZDO zdo = movingPiece.m_nview.GetZDO();
        if (zdo != null)
        {
            zdo.SetPosition(pos);
            zdo.SetRotation(rot);
        }
        if (doAttack)
        {
            ItemDrop.ItemData item = player.GetRightItem();
            if (item != null)
            {
                player.FaceLookDirection();
                player.m_zanim.SetTrigger(item.m_shared.m_attack.m_attackAnimation);
            }
        }

        movingPiece.m_placeEffect.Create(pos, rot, movingPiece.transform);
        
        movingPiece = null;
        player.SetSelectedPiece(Tool.piece);
        player.SetupPlacementGhost();
        return true;
    }
}