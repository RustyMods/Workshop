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
            GameObject prefab = PrefabManager.GetPrefab(Utils.GetPrefabName(hovering.name));
            if (prefab == null) return;
            
            ISelectMany.tempPiece = SetupPiece(prefab);
            player.SetupPlacementGhost();
        }
    }

    private static Piece SetupPiece(GameObject obj)
    {
        GameObject container = new GameObject(obj.name);
        container.transform.SetParent(MockManager.transform);
        container.transform.position = Vector3.zero;
        container.transform.rotation = Quaternion.identity;
        
        Piece piece = container.AddComponent<Piece>();
        piece.m_icon = BuildTools.planHammer.GetIcon();
        piece.m_name = obj.name;
        piece.m_category = Piece.PieceCategory.Misc;
        piece.m_extraPlacementDistance = 100;
        container.AddComponent<PlanContainer>();
        
        ZNetView.m_forceDisableInit = true;
        TerrainOp.m_forceDisableTerrainOps = true;
        GameObject instance = Object.Instantiate(obj, container.transform);
        ZNetView.m_forceDisableInit = false;
        TerrainOp.m_forceDisableTerrainOps = false;
        instance.SetActive(false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.name = obj.name;
        piece.m_resources = TryGetPieceRequirements(obj);
            
        instance.RemoveAllComponents();
        instance.SetActive(true);

        return piece;
    }

    public static Piece.Requirement[] TryGetPieceRequirements(GameObject gameObject)
    {
        if (gameObject == null) return Array.Empty<Piece.Requirement>();
        
        if (gameObject.TryGetComponent(out DropOnDestroyed dropOnDestroyed))
        {
            return ToPieceRequirements(dropOnDestroyed.m_dropWhenDestroyed);
        }
        if (gameObject.TryGetComponent(out TreeBase tree))
        {
            return ToPieceRequirements(tree.m_dropWhenDestroyed);
        }
        if (gameObject.TryGetComponent(out MineRock5 mineRock5))
        {
            return ToPieceRequirements(
                mineRock5.m_dropItems, 
                gameObject.GetComponentsInChildren<Collider>().Length);
        }
        if (gameObject.TryGetComponent(out MineRock mineRock))
        {
            return ToPieceRequirements(
                mineRock.m_dropItems, 
                gameObject.GetComponentsInChildren<Collider>().Length);
        }

        if (gameObject.TryGetComponent(out Destructible destructible) &&
            destructible.m_spawnWhenDestroyed != null &&
            destructible.m_spawnWhenDestroyed.TryGetComponent(out mineRock5))
        {
            return ToPieceRequirements(
                mineRock5.m_dropItems, 
                destructible.m_spawnWhenDestroyed.GetComponentsInChildren<Collider>().Length);
        }
        
        return Array.Empty<Piece.Requirement>();
    }

    public static Piece.Requirement[] ToPieceRequirements(DropTable table, int multiplier = 1)
    {
        List<Piece.Requirement> requirements = new List<Piece.Requirement>();
        foreach (DropTable.DropData drop in table.m_drops)
        {
            if (drop.m_item.TryGetComponent(out ItemDrop item))
            {
                requirements.Add(new Piece.Requirement
                {
                    m_resItem = item,
                    m_amount = drop.m_stackMax * multiplier
                });
            }
        }
        return requirements.ToArray();
    }

    private static bool IsSelectPiece(Piece piece) => piece != null && Tools.Contains(piece);
}