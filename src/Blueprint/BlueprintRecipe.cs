using System;
using System.IO;
using UnityEngine;

namespace Workshop;

public class BlueprintRecipe : Recipe
{
    public Blueprint blueprint;
    public readonly BlueprintSettings settings = new BlueprintSettings();

    public GameObject prefab;
    public Sprite icon;
    public Piece piece;
    
    public bool Loaded;
    public bool doSnapshot;
    
    public void Dispose()
    {
        if (!Loaded) return;
        UnityEngine.Object.Destroy(prefab);
        UnityEngine.Object.Destroy(icon);
        prefab = null;
        icon = null;
        piece = null;
        Loaded = false;
    }
    
    public GameObject Load()
    {
        if (Loaded) return null;

        Workshop.LogDebug($"Blueprint Loading: {name}");
        prefab = new GameObject(settings.Name);
        prefab.SetActive(false);
        prefab.transform.SetParent(MockManager.transform);
        prefab.transform.position = Vector3.zero;
        prefab.transform.rotation = Quaternion.identity;
        PlanContainer planContainer = prefab.AddComponent<PlanContainer>();
        planContainer.m_recipe = this;
        MockManager.temp.Add(prefab);

        piece = prefab.AddComponent<Piece>();
        piece.m_name = settings.Name;
        piece.m_category = PieceTableMan.GetCategory("Blueprints");
        piece.m_extraPlacementDistance = 100;
        
        SetupResources();
        
        int missing = 0;
        bool hasMissingPieces = false;

        for (int i = 0; i < settings.Pieces.Count; ++i)
        {
            PlanPiece planPiece = settings.Pieces[i];
            GameObject instance = planPiece.Create(prefab.transform, i);
            if (instance == null)
            {
                Workshop.LogDebug($"[ Blueprint {settings.Name} ]: {planPiece.PrefabId} not found");
                ++missing;
                hasMissingPieces = true;
            }
        }

        if (!string.IsNullOrEmpty(settings.Center))
        {
            Transform center = Utils.FindChild(prefab.transform, settings.Center);
            if (center != null)
            {
                Vector3 position = center.localPosition;
                for (int i = 0; i < prefab.transform.childCount; ++i)
                {
                    Transform child = prefab.transform.GetChild(i);
                    child.transform.localPosition -= position;
                }
            }
        }
        
        if (hasMissingPieces)
        {
            piece.m_description += $"{settings.Description}\n[ <color=red>unavailable pieces: {missing}</color> ]";
        }
        else
        {
            piece.m_description = settings.Description;
        }

        for (int i = 0; i < settings.Terrains.Count; ++i)
        {
            PlanTerrain terrain = settings.Terrains[i];
            terrain.Create(prefab.transform, i);
        }

        for (int i = 0; i < settings.SnapPoints.Count; ++i)
        {
            PlanSnapPoint point = settings.SnapPoints[i];
            point.Create(prefab.transform, i);
        }

        if (string.IsNullOrEmpty(settings.Icon)) settings.Icon = prefab.name + ".png";
        
        if (BlueprintMan.icons.TryGetValue(settings.Icon, out byte[] bytes))
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.LoadImage4x(bytes);
            tex.Apply();
            tex.name = settings.Icon;
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            sprite.name = settings.Icon;
            piece.m_icon = sprite;
            icon = sprite;
            Workshop.LogDebug($"Created icon from imported PNG for {prefab.name}");
        }
        else
        {
            piece.m_icon = BuildTools.BlueprintIcon;
            doSnapshot = true;
        }

        m_item = BlueprintScroll.Create(this);

        piece.m_resources = new[]
        {
            new Piece.Requirement
            {
                m_resItem = m_item,
                m_amount = 1,
                m_recover = false
            }
        };

        m_craftingStation = BlueprintTable.CRAFTING_STATION;
        m_repairStation = BlueprintTable.CRAFTING_STATION;
        
        this.Register();
        Loaded = true;
        prefab.SetActive(true);
        return prefab;
    }

    public void SetupResources()
    {
        if (string.IsNullOrEmpty(settings.Requirements))
        {
            m_resources = Array.Empty<Piece.Requirement>();
        }
        else
        {
            string[] parts = settings.Requirements.Split(';');
            for (int i = 0; i < parts.Length; ++i)
            {
                string section = parts[i];
                string[] info = section.Split(',');
                string item = info.GetString(0);
                if (string.IsNullOrEmpty(item)) continue;
                int amount = info.GetInt(1, 1);
                settings.requirements.Add(new Requirement(item, amount));
            }
            m_resources = new PieceRequirements(settings.requirements).ToPieceRequirement(name);
        }
    }
    
    public void PostProcess()
    {
        if (!Loaded) return;

        if (!doSnapshot) return;
        if (!Snapshot.TryCreate(prefab, out Sprite snap, 0.8f)) return;
        
        doSnapshot = false;
        piece.m_icon = snap;
        icon = snap;
        icon.name = prefab.name;
        settings.Icon = prefab.name;
        byte[] bytes = icon.texture.EncodeToPNG();
        string filename = prefab.name + ".png";
        string filePath = Path.Combine(BlueprintMan.GetIconPath(), filename);
        File.WriteAllBytes(filePath, bytes);
        BlueprintMan.icons[filename] = bytes;
        
        Workshop.LogDebug($"Generated icon for {prefab.name}");
    }
}