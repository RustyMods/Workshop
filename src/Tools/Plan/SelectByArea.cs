using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Workshop;

public class SelectByArea : ISelectMany
{
    public static readonly List<SelectByArea> Tools = new List<SelectByArea>();
    private static readonly List<Piece> AreaPieces = new List<Piece>();
    protected readonly AreaProjector projector;
    
    public static bool IsAreaPiece(Piece piece) => AreaPieces.Contains(piece);

    public static void SetRepairMode(bool enable)
    {
        for (int i = 0; i < Tools.Count; i++)
        {
            SelectByArea tool = Tools[i];
            tool.piece.m_repairPiece = enable;
        }
    }
    
    public SelectByArea(string id, string name, int index = 1) : base(id, name, index)
    {
        GameObject guardStone = PrefabManager.GetPrefab("dverger_guardstone");
        PrivateArea area = guardStone.GetComponent<PrivateArea>();
        GameObject marker = area.m_areaMarker.gameObject;
        GameObject instance = Object.Instantiate(marker, piece.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.name = "_GhostOnly";
        instance.SetActive(false);
        projector = piece.gameObject.AddComponent<AreaProjector>();
        piece.m_canRotate = false;
        AreaPieces.Add(piece);
        piece.m_icon = SpriteManager.GetSprite("select_area_icon.png");
        Tools.Add(this);
    }

    protected override bool TryGetSelection(Player player, out List<ZNetView> objects) => TryGetInArea(player, out objects);
    
    public static bool TryGetInArea(Player player, out List<ZNetView> objects)
    {
        objects = new List<ZNetView>();

        if (AreaProjector.instance == null) return false;

        List<ZNetView> instances = ZNetScene.instance.m_instances.Values.ToList();
        for (int i = 0; i < instances.Count; ++i)
        {
            if (objects.Count >= MaxSelectionSize) break;
            ZNetView instance = instances[i];
            float distance = Utils.DistanceXZ(instance.transform.position, AreaProjector.instance.transform.position);
            if (distance > AreaProjector.radius || !instance.GetComponent<Selectable>()) continue;
            objects.Add(instance);
        }

        return objects.Count > 0;
    }
}