using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public abstract class ISelectMany : ITool
{
    protected const int MaxSelectionSize = 6000;

    public static readonly List<Piece> SelectTools = new();
    public static readonly List<Piece> RemoveTools = new();
    public static Piece tempPiece;

    protected ISelectMany(string id, string name, int index = 0) : base(id, name, index)
    {
        SelectTools.Add(piece);
    }

    protected abstract bool TryGetSelection(Player player, out List<ZNetView> objects);
    
    public override void OnUse(Player player)
    {
        if (!TryGetSelection(player, out List<ZNetView> objects)) return;
        objects.RemoveAll(x => x.GetComponent<Character>() || x.GetComponent<ConstructionWard>());
        player.Message(MessageHud.MessageType.Center, $"Selected ({objects.Count}) objects");
        tempPiece = PlanContainer.Setup(objects);
        player.SetupPlacementGhost();
    }
    
    public static bool OnPlace(Player player, Piece piece, Vector3 pos, Quaternion rot, bool doAttack)
    {
        if (!piece.IsBlueprint() || player.m_placementGhost == null) return false;
        for (int i = 0; i < player.m_placementGhost.transform.childCount; ++i)
        {
            Transform child = player.m_placementGhost.transform.GetChild(i);
            GameObject prefab = ZNetScene.instance.GetPrefab(child.name);
            if (prefab == null) continue;
            GhostPiece.PlacePiece(player, prefab, child.position, child.rotation, false, child.GetComponent<Plan>());
        }
        return true;
    }
}