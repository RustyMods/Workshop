using System.Collections.Generic;
using HarmonyLib;
using LocalizationManager;
using UnityEngine;

namespace Workshop;

public static class PrefabManager
{
    private static ZNetScene _ZNetScene;
    private static ObjectDB _ObjectDB;
    private static readonly List<GameObject> PrefabsToRegister = new  List<GameObject>();
    private static readonly List<Recipe> RecipesToRegister = new  List<Recipe>();
    private static readonly Dictionary<string, ItemDrop> ItemsBySharedName = new();
    
    public static void RegisterPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        
        if (ZNetScene.instance)
        {
            ZNetScene.instance.Register(prefab);
        }
        
        if (ObjectDB.instance)
        {
            ObjectDB.instance.Register(prefab);
        }
        
        PrefabsToRegister.Add(prefab);
    }

    public static void UnRegisterPrefab(this GameObject prefab)
    {
        if (prefab == null) return;
        if (ZNetScene.instance) ZNetScene.instance.UnRegister(prefab);
        if (ObjectDB.instance) ObjectDB.instance.UnRegister(prefab);
        if (_ZNetScene != null) _ZNetScene.UnRegister(prefab);
        if (_ObjectDB != null) _ObjectDB.UnRegister(prefab);
        PrefabsToRegister.Remove(prefab);
    }

    public static Recipe GetRecipe(ItemDrop.ItemData item)
    {
        if (ObjectDB.instance) return ObjectDB.instance.GetRecipe(item);
        if (_ObjectDB != null) return _ObjectDB.GetRecipe(item);
        return null;
    }

    public static void Register(this Recipe recipe)
    {
        if (ObjectDB.instance)
        {
            if (!ObjectDB.instance.m_recipes.Contains(recipe))
            {
                ObjectDB.instance.m_recipes.Add(recipe);
            }
        }
        if (_ObjectDB != null)
        {
            if (!_ObjectDB.m_recipes.Contains(recipe))
            {
                _ObjectDB.m_recipes.Add(recipe);
            }
        }

        if (!RecipesToRegister.Contains(recipe))
        {
            RecipesToRegister.Add(recipe);
        }
    }

    public static void UnRegister(this Recipe recipe)
    {
        if (ObjectDB.instance)
        {
            ObjectDB.instance.m_recipes.Remove(recipe);
        }

        if (_ObjectDB != null)
        {
            _ObjectDB.m_recipes.Remove(recipe);
        }
        
        RecipesToRegister.Remove(recipe);
    }

    private static void Register(this ZNetScene scene, GameObject prefab)
    {
        if (scene.m_prefabs.Contains(prefab) || !prefab.GetComponent<ZNetView>()) return;
        scene.m_prefabs.Add(prefab);
        scene.m_namedPrefabs[prefab.name.GetStableHashCode()] = prefab;
    }

    public static void UnRegister(this ZNetScene scene, GameObject prefab)
    {
        scene.m_prefabs.Remove(prefab);
        scene.m_namedPrefabs.Remove(prefab.name.GetStableHashCode());
    }

    private static void Register(this ObjectDB db, GameObject prefab)
    {
        if (db.m_items.Contains(prefab) || !prefab.GetComponent<ItemDrop>()) return;
        db.m_items.Add(prefab);
        db.m_itemByHash[prefab.name.GetStableHashCode()] = prefab;
    }

    public static void UnRegister(this ObjectDB db, GameObject prefab)
    {
        db.m_items.Remove(prefab);
        db.m_itemByHash.Remove(prefab.name.GetStableHashCode());
    }

    public static bool TryGetItemDrop(string sharedName, out ItemDrop item) => ItemsBySharedName.TryGetValue(sharedName, out item);

    internal static GameObject GetPrefab(string prefabName)
    {
        GameObject prefab;
        
        if (ZNetScene.instance != null)
        {
            prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (prefab != null) return prefab;
        }

        if (_ZNetScene != null)
        {
            prefab = _ZNetScene.m_prefabs.Find(p => p.name == prefabName);
            if (prefab != null) return prefab;
        }

        return null;
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    public static class FejdStartup_Awake_Patch
    {
        private static void Prefix(FejdStartup __instance)
        {
            _ZNetScene = __instance.m_objectDBPrefab.GetComponent<ZNetScene>();
            _ObjectDB = __instance.m_objectDBPrefab.GetComponent<ObjectDB>();
            BuildTools.OnFejdStartup();
            MockManager.OnFejdStartup();
            // ShaderMan.OnFejdStartup();
            ConfigManager.OnFejdStartup();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.UpdateRegisters))]
    private static class ObjectDB_CopyOtherDB_Patch
    {
        private static void Prefix(ObjectDB __instance)
        {
            foreach (GameObject prefab in PrefabsToRegister)
            {
                if (!prefab.GetComponent<ItemDrop>() || __instance.m_items.Contains(prefab)) continue;
                __instance.m_items.Add(prefab);
            }
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    private static class ObjectDB_Awake_Patch
    {
        private static void Prefix(ObjectDB __instance)
        {
            foreach (GameObject prefab in PrefabsToRegister)
            {
                if (!prefab.GetComponent<ItemDrop>() || __instance.m_items.Contains(prefab)) continue;
                __instance.m_items.Add(prefab);
            }

            foreach (Recipe recipe in RecipesToRegister)
            {
                if (__instance.m_recipes.Contains(recipe)) continue;
                __instance.m_recipes.Add(recipe);
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ObjectDB __instance)
        {
            for (int i = 0; i < __instance.m_items.Count; ++i)
            {
                GameObject item = __instance.m_items[i];
                if (!item.TryGetComponent(out ItemDrop component)) continue;
                ItemsBySharedName[component.m_itemData.m_shared.m_name] = component;
            }
        }
    }
    
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class ZNetScene_Awake_Patch
    {
        private static void Prefix(ZNetScene __instance)
        {
            foreach (GameObject prefab in PrefabsToRegister)
            {
                if (!prefab.GetComponent<ZNetView>() || __instance.m_prefabs.Contains(prefab)) continue;
                __instance.m_prefabs.Add(prefab);
            }

            Localizer.Load();
        }
        
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ZNetScene __instance)
        {
            for (int i = 0; i < __instance.m_prefabs.Count; ++i)
            {
                GameObject prefab = __instance.m_prefabs[i];
                if (prefab.GetComponent<Character>() || prefab.GetComponentsInChildren<Renderer>().Length <= 0) continue;
                prefab.AddComponent<Selectable>();
                if (prefab.GetComponent<Piece>())
                {
                    BuildTools.ghostHammer.AddPiece(prefab);
                }
            }
            
            BuildTools.ghostHammer.RemoveAll<Ship>();
            BuildTools.ghostHammer.RemoveAll<Plant>();
            
            BlueprintMan.Load(BuildTools.planHammer.table);
        }
    }
}