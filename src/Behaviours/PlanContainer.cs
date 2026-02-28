using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Workshop;

public class PlanContainer : MonoBehaviour
{
    public static PlanContainer instance;
    
    public Vector3 m_placementModifier = Vector3.zero;
    public float m_lowestPoint;
    public Piece m_piece;
    public BlueprintRecipe m_recipe;
    public List<Transform> m_snapPoints = new();
    public List<Piece> m_pieces = new();
    public List<Selectable> m_selectables = new();
    public List<PlanTerrain> m_terrains = new();
    public List<Renderer> m_renderers = new();
    public List<MeshFilter> m_meshFilters = new();
    public bool isLocalPlayerCreator;
    public void Awake()
    {
        m_piece = GetComponent<Piece>();
        m_piece.GetSnapPoints(m_snapPoints);
        m_pieces.AddRange(GetComponentsInChildren<Piece>());
        m_selectables.AddRange(GetComponentsInChildren<Selectable>());
        m_renderers.AddRange(GetComponentsInChildren<Renderer>());
        m_meshFilters.AddRange(GetComponentsInChildren<MeshFilter>());
        FindLowestPoint();
        instance = this;
    }

    public void Start()
    {
        isLocalPlayerCreator = m_recipe == null || 
                               m_recipe.settings.Creator.Equals(Game.instance.m_playerProfile.m_playerName);
    }

    public void OnDestroy()
    {
        instance = null;
    }

    public bool IsInside(Selectable selectable) => m_selectables.Contains(selectable);

    private void FindLowestPoint()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            Transform child = transform.GetChild(i);
            if (m_snapPoints.Contains(child)) continue;
            if (child.transform.localPosition.y < m_lowestPoint)
            {
                m_lowestPoint = child.transform.localPosition.y;
            }
        }
    }

    public void SetPosition(Vector3 pos)
    {
        transform.position = pos + new Vector3(0f, -m_lowestPoint, 0f) + m_placementModifier;
    }

    public void Update()
    {
        if (ZInput.GetKeyDown(ConfigManager.SaveKeyCode) && isLocalPlayerCreator)
        {
            Workshop.instance.Save(m_piece, m_pieces, m_recipe);
        }
        
        if (Player.m_localPlayer && ZInput.GetKey(ConfigManager.ResetManualSnapKey))
        {
            Player.m_localPlayer.m_manualSnapPoint = -1;
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_snapping $msg_snapping_auto");
        }
    
        bool changed = false;
        if (ZInput.GetKey(KeyCode.UpArrow))
        {
            m_placementModifier.y += ConfigManager.PlacementIncrement;
            changed = true;
        }
    
        if (ZInput.GetKey(KeyCode.DownArrow))
        {
            m_placementModifier.y -= ConfigManager.PlacementIncrement;
            changed = true;
        }
    
        if (ZInput.GetKey(KeyCode.LeftArrow))
        {
            m_placementModifier.x -= ConfigManager.PlacementIncrement;
            changed = true;
        }
    
        if (ZInput.GetKey(KeyCode.RightArrow))
        {
            m_placementModifier.x += ConfigManager.PlacementIncrement;
            changed = true;
        }
    
        if (changed && Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, 
                $"{name}: {m_placementModifier.x:0.0}, {m_placementModifier.y:0.0}, {m_placementModifier.z:0.0}");   
        }
    }
    
    private static Vector3 GetCenter(List<ZNetView> objects)
    {
        Bounds bounds = new Bounds(objects[0].transform.position, Vector3.one);
        for (int i = 1; i < objects.Count; ++i)
        {
            ZNetView znv = objects[i];
            Vector3 position = znv.transform.position;
            bounds.Encapsulate(position);
        }

        return bounds.center;
    }
    
    public static Piece Setup(List<ZNetView> objects)
    {
        string prefabName = $"Pieces ({objects.Count})";
        GameObject container = new GameObject(prefabName);
        container.SetActive(false);
        container.transform.SetParent(MockManager.transform);
        container.transform.position = Vector3.zero;
        container.transform.rotation = Quaternion.identity;
        var plan = container.AddComponent<PlanContainer>();
        MockManager.temp.Add(container);
        
        Vector3 groupCenter = GetCenter(objects);
        
        Dictionary<string, int> count = new Dictionary<string, int>();
        
        for (int i = 0; i < objects.Count; ++i)
        {
            ZNetView child = objects[i];
            ZDO zdo = child.GetZDO();
            if (zdo == null)
            {
                Workshop.LogWarning("plan container setup: ZDO is null");
                continue;
            }
            GameObject prefab = ZNetScene.instance.GetPrefab(zdo.m_prefab);
            if (prefab == null)
            {
                Workshop.LogWarning($"[Object: {child.name}]: prefab is null");
                continue;
            }
            ZNetView.m_forceDisableInit = true;
            TerrainOp.m_forceDisableTerrainOps = true;
            GameObject go = Instantiate(prefab, container.transform);
            ZNetView.m_forceDisableInit = false;
            TerrainOp.m_forceDisableTerrainOps = false;
            go.SetActive(false);
            go.name = prefab.name;
            go.transform.localPosition = child.transform.position - groupCenter;
            go.transform.localRotation = child.transform.rotation;
            go.transform.localScale = child.transform.localScale;
            
            ZPackage pkg = new ZPackage();
            zdo.Serialize(pkg);
            string base64 = pkg.GetBase64();
            go.RemoveAllComponents();
            Plan temp = go.AddComponent<Plan>();
            temp.zdo = base64;
            if (child.TryGetComponent(out Container cont))
            {
                temp.width = cont.m_width;
                temp.height = cont.m_height;
            }
            
            go.SetActive(true);
            MoveSnapPoint(go, container.transform);
            
            if (count.ContainsKey(prefab.name)) count[prefab.name]++;
            else count.Add(prefab.name, 1);
        }
        
        if (AreaProjector.instance != null && 
            TerrainModifiers.TryFindTerrain(
                AreaProjector.instance.transform.position, 
                AreaProjector.radius,
                out List<PlanTerrain> terrainComps))
        {
            plan.m_terrains = terrainComps;
            count["_TerrainModifiers"] = terrainComps.Count;
            for (int i = 0; i < terrainComps.Count; ++i)
            {
                PlanTerrain terrain = terrainComps[i];
                GameObject go = terrain.Create(container.transform, i);
                go.transform.localPosition -= groupCenter;
            }
        }

        Piece component = container.AddComponent<Piece>();
        component.m_icon = BuildTools.BlueprintIcon;
        component.m_name = prefabName;
        component.m_description = string.Join(", ", count.Select(kvp => $"{kvp.Key} x{kvp.Value}"));
        component.m_category = Piece.PieceCategory.Misc;
        component.m_resources = Array.Empty<Piece.Requirement>();
        component.m_extraPlacementDistance = 100;
        container.SetActive(true);
        
        return component;
    }
    
    private static void MoveSnapPoint(GameObject go, Transform parent)
    {
        if (!go.TryGetComponent(out Piece piece)) return;
        List<Transform> children = new();
        piece.GetSnapPoints(children);
        for (int i = 0; i < children.Count; ++i)
        {
            Transform child = children[i];
            child.SetParent(parent);
        }
    }
}