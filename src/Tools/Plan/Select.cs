using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Workshop;

public class Select : ITool
{
    public static GameObject hovering;
    private static readonly RaycastHit[] raycastHits = new RaycastHit[64];
    private static readonly List<Piece> Tools = new();
    private const float maxDistance = 50f;
    
    public Select(string id, string name, int index = 1) : base(id, name, index)
    {
        ISelectMany.SelectTools.Add(piece);
        Tools.Add(piece);
        piece.m_icon = SpriteManager.GetSprite("select_icon.png");
    }

    public static void UpdateHover(Player player)
    {
        hovering = null;

        int size = Physics.RaycastNonAlloc(
            GameCamera.instance.transform.position,
            GameCamera.instance.transform.forward, 
            raycastHits, 
            maxDistance,
            player.m_placeRayMask);
        
        Array.Sort(raycastHits, 0, size, Player.RaycastHitComparer.Instance);

        for (int i = 0; i < size; ++i)
        {
            RaycastHit hit = raycastHits[i];
            Selectable selectable = hit.collider.GetComponentInParent<Selectable>();
            if (selectable == null) continue;
            hovering = selectable.gameObject;
            player.m_hoveringPiece = selectable.m_piece;
            selectable.Highlight(ConfigManager.HighlightColor);
            break;
        }
    }

    public override void OnUse(Player player)
    {
        if (hovering == null) return;
        if (hovering.TryGetComponent(out Piece hoverPiece))
        {
            player.SetSelectedPiece(hoverPiece);
        }
        else
        {
            ISelectMany.tempPiece = SetupPiece(hovering);
            player.SetupPlacementGhost();
        }
    }

    private static Piece SetupPiece(GameObject obj)
    {
        string id = Utils.GetPrefabName(obj.name);
        GameObject prefab = ZNetScene.instance.GetPrefab(id);
        
        ZNetView.m_forceDisableInit = true;
        GameObject container = new GameObject(id);
        container.transform.SetParent(MockManager.transform);
        container.transform.position = Vector3.zero;
        container.transform.rotation = Quaternion.identity;
        
        container.AddComponent<ZNetView>();
        container.AddComponent<WearNTear>();
        Piece component = container.AddComponent<Piece>();
        component.m_icon = BuildTools.planHammer.GetIcon();
        component.m_name = id;
        component.m_category = Piece.PieceCategory.Misc;
        component.m_extraPlacementDistance = 100;
        container.AddComponent<PlanContainer>();
        
        if (prefab != null)
        {
            GameObject instance = Object.Instantiate(prefab, container.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.name = id;

            if (instance.TryGetComponent(out DropOnDestroyed dropOnDestroyed))
            {
                component.m_resources = ToPieceRequirements(dropOnDestroyed.m_dropWhenDestroyed);
            }
            else if (instance.TryGetComponent(out TreeBase tree))
            {
                component.m_resources = ToPieceRequirements(tree.m_dropWhenDestroyed);
            }
            else
            {
                component.m_resources = Array.Empty<Piece.Requirement>();
            }
        }
        else
        {
            component.m_resources = Array.Empty<Piece.Requirement>();
        }
        ZNetView.m_forceDisableInit = false;
        
        return component;
    }

    private static Piece.Requirement[] ToPieceRequirements(DropTable table)
    {
        List<Piece.Requirement> requirements = new List<Piece.Requirement>();
        foreach (DropTable.DropData drop in table.m_drops)
        {
            if (drop.m_item.TryGetComponent(out ItemDrop item))
            {
                requirements.Add(new Piece.Requirement
                {
                    m_resItem = item,
                    m_amount = drop.m_stackMax
                });
            }
        }
        return requirements.ToArray();
    }

    private static bool IsSelectPiece(Piece piece) => piece != null && Tools.Contains(piece);
}