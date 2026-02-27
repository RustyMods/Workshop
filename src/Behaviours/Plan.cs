using System;
using UnityEngine;

namespace Workshop;

public class Plan : MonoBehaviour
{
    public string zdo = "";
    public string attach = "";
    public int state;
    public ItemStandItemData itemStand;
    public int width = 5;
    public int height = 8;
    public Inventory inventory;
    
    public TerrainModifier.PaintType m_type;
    public bool m_isSquare;
    public float m_radius;
    public float m_smoothRadius;
    public bool m_level;

    public void Start()
    {
        inventory = new Inventory("PlanInventory", null, width, height);
        itemStand = new ItemStandItemData(attach);
        ReadZDO();
    }

    public void ReadZDO()
    {
        if (string.IsNullOrEmpty(zdo)) return;
        ZDO ZDO = new ZDO();
        try
        {
            ZPackage pkg = new ZPackage(zdo);
            ZDO.Deserialize(pkg);
            string items = ZDO.GetString(ZDOVars.s_items);
            if (!string.IsNullOrEmpty(items))
            {
                GameObject original = PrefabManager.GetPrefab(name);
                if (original == null) return;
                if (!original.GetComponent<Container>()) return;
                ZPackage itemPkg = new ZPackage(items);
                inventory.Load(itemPkg);
            }
        }
        catch
        {
            zdo = "";
        }
    }
}

[Serializable]
public class ItemStandItemData
{
    public readonly string prefab;
    public readonly int variant;
    public readonly int quality;

    public bool isValid => !string.IsNullOrEmpty(prefab);

    public ItemStandItemData(string line)
    {
        string[] parts = line.Split(':');
        prefab = parts.GetString(0);
        variant = parts.GetInt(1);
        quality = parts.GetInt(1, 1);
    }

    public ItemStandItemData(string prefab, int variant, int quality)
    {
        this.prefab = prefab;
        this.variant = variant;
        this.quality = quality;
    }

    public bool TryGetPieceRequirement(out Piece.Requirement requirement)
    {
        requirement = null;
        var source = PrefabManager.GetPrefab(prefab);
        if (source == null) return false;
        if (!source.TryGetComponent(out ItemDrop component)) return false;
        requirement = new Piece.Requirement
        {
            m_resItem = component,
            m_amount = 1
        };
        return true;
    }
    
    public override string ToString() => $"{prefab}:{variant}:{quality}";
}