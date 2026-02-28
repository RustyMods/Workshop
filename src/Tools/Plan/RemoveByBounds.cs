using System.Collections.Generic;

namespace Workshop;

public class RemoveByBounds : SelectByBounds
{
    public RemoveByBounds(string id, string name, int index = 1) : base(id, name, index)
    {
        RemoveTools.Add(piece);
        adminOnly = true;
        piece.m_icon = SpriteManager.GetSprite("remove_bounds_icon.png");
    }

    public override void OnUse(Player player)
    {
        if (!TryGetSelection(player, out List<ZNetView> pieces)) return;
        
        for (int i = 0; i < pieces.Count; ++i)
        {
            ZNetView component = pieces[i];
            component.ClaimOwnership();
            component.Destroy();
        }
        player.Message(MessageHud.MessageType.Center, $"Removed ({pieces.Count}) pieces");
    }
}