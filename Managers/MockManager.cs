using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Workshop;

public static class MockManager
{
    private static readonly GameObject root;
    public static Transform transform => root.transform;
    internal static readonly Dictionary<string, GameObject> prefabs;
    internal static readonly Dictionary<string, Mock> mocks;
    public static readonly List<GameObject> temp = new();

    static MockManager()
    {
        root = new GameObject("MockManager");
        Object.DontDestroyOnLoad(root);
        root.SetActive(false);
        
        prefabs = new Dictionary<string, GameObject>();
        mocks = new Dictionary<string, Mock>();
    }

    public static void OnFejdStartup()
    {
        foreach (Mock mock in mocks.Values)
        {
            mock.Create();
        }
    }

    public static void Clear()
    {
        foreach (var kvp in prefabs)
        {
            Object.Destroy(kvp.Value);
        }
        prefabs.Clear();
    }

    public static void Remove<T>(this GameObject prefab) where T : MonoBehaviour
    {
        if (!prefab.TryGetComponent(out T component)) return;
        Object.DestroyImmediate(component);
    }

    private static void DisableAllComponents(this GameObject prefab, params GameObject[] ignorePrefabs)
    {
        List<GameObject> prefabsToIgnore = ignorePrefabs.ToList();
        MonoBehaviour[] components = prefab.GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < components.Length; ++i)
        {
            MonoBehaviour component = components[i];
            if (prefabsToIgnore.Contains(component.gameObject)) continue;
            component.enabled = false;
        }
    }

    public static void RemoveAllComponents(this GameObject prefab)
    {
        MonoBehaviour[] components = prefab.GetComponentsInChildren<MonoBehaviour>();
        for (int i = 0; i < components.Length; ++i)
        {
            MonoBehaviour component = components[i];
            if (component is Piece) continue;
            if (component is Selectable) continue;
            Object.Destroy(component);
        }
    }
}

public class Mock
{
    private GameObject source;
    public GameObject prefab;
    private readonly string sourceId;
    private readonly string prefabId;
    public event Action<GameObject> OnCreated;
    private bool loaded;

    public Mock(string sourceId, string prefabId)
    {
        this.sourceId = sourceId;
        this.prefabId = prefabId;
        MockManager.mocks[prefabId] = this;
    }

    public Mock(GameObject source, string prefabId) : this(source.name, prefabId)
    {
        this.source = source;
    }

    public GameObject Create()
    {
        if (loaded) return prefab;
        source ??= PrefabManager.GetPrefab(sourceId);
        if (source == null) return null;
        prefab = Object.Instantiate(source, MockManager.transform, false);
        prefab.name = prefabId;
        OnCreated?.Invoke(prefab);
        PrefabManager.RegisterPrefab(prefab);
        MockManager.prefabs[prefabId] = prefab;
        loaded = true;
        return prefab;
    }
}