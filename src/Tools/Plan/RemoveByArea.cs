using System.Collections.Generic;

namespace Workshop;

public class RemoveByArea : SelectByArea
{
    public RemoveByArea(string id, string name, int index = 1) : base(id, name, index)
    {
        RemoveTools.Add(piece);
        projector.targetPiecesOnly = false;
        adminOnly = true;
        piece.m_icon = SpriteManager.GetSprite("remove_area_icon.png");
    }

    public override void OnUse(Player player)
    {
        if (!TryGetSelection(player, out List<ZNetView> objects)) return;
        objects.RemoveAll(obj => obj.GetComponent<Character>());
        for (int i = 0; i < objects.Count; ++i)
        {
            ZNetView component = objects[i];
            component.ClaimOwnership();
            component.Destroy();
        }
        player.Message(MessageHud.MessageType.Center, $"Removed ({objects.Count}) objects");
    }

    protected override bool TryGetSelection(Player player, out List<ZNetView> objects) => TryGetInArea(player, out objects);
}