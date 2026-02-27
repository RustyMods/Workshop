using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public static class BlueprintScroll
{
    private static readonly GameObject scroll = AssetBundleManager.LoadAsset<GameObject>("buildtoolbundle", "BlueprintBase");
    public static Sprite icon;

    static BlueprintScroll()
    {
        icon = scroll.GetComponent<ItemDrop>().m_itemData.GetIcon();
    }

    public static ItemDrop Create(BlueprintRecipe recipe)
    {
        GameObject prefab = Object.Instantiate(scroll, MockManager.transform);
        prefab.name = recipe.settings.Name + "_Blueprint_Scroll";
        
        ItemDrop component = prefab.GetComponent<ItemDrop>();
        component.m_itemData.m_shared.m_name = recipe.settings.Name;
        component.m_itemData.m_shared.m_description = $"Required resource to place blueprint {recipe.settings.Name}";
        component.m_itemData.m_shared.m_weight = 0f;
        component.m_itemData.m_shared.m_useDurability = false;

        prefab.AddComponent<Scroll>();
        
        PrefabManager.RegisterPrefab(prefab);
        return component;
    }
}

public class Scroll : MonoBehaviour
{
    private static readonly List<Scroll> instances = new();
    public void Awake()
    {
        instances.Add(this);
    }
    
    //TODO: add system to delete scrolls from world if removed by server.

    public void OnDestroy()
    {
        instances.Remove(this);
    }

    public static List<Scroll> GetAllScrolls() => instances;
}