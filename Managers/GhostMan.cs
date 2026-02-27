using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Workshop;

public static class GhostMan
{
    public static readonly Material GhostMat = AssetBundleManager.LoadAsset<Material>("buildtoolbundle", "GhostMat");

    private static readonly Dictionary<int, GhostBlock> blocks = new();
    public static bool enabled = true;
    private static float lastToggleTime;

    public static void Register(GameObject go)
    {
        if (blocks.ContainsKey(go.GetInstanceID())) return;

        MeshRenderer[] meshRenderers = go.GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skinRenderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();

        List<Renderer> renderers = new List<Renderer>();
        renderers.AddRange(meshRenderers);
        renderers.AddRange(skinRenderers);
        
        GhostBlock block = new GhostBlock(renderers);
        blocks[go.GetInstanceID()] = block;

        go.AddComponent<Ghost>();
    }

    public static void UpdateToggle()
    {
        if (ZInput.GetKey(ConfigManager.ToggleGhost))
        {
            Toggle();
        }
    }

    private static void Toggle()
    {
        if (Time.time - lastToggleTime < 1f) return;
        
        foreach (GhostBlock block in blocks.Values)
        {
            if (enabled)
            {
                block.Reset();
            }
            else
            {
                block.ReApply();
            }
        }

        enabled = !enabled;
        lastToggleTime = Time.time;
    }

    public static void Reset(GameObject go)
    {
        if (blocks.TryGetValue(go.GetInstanceID(), out GhostBlock block))
        {
            block.Reset();
        }
    }

    public static void ReApply(GameObject go)
    {
        if (blocks.TryGetValue(go.GetInstanceID(), out GhostBlock block))
        {
            block.ReApply();
        }
    }

    public static void UnRegister(GameObject go)
    {
        blocks.Remove(go.GetInstanceID());
    }

    public static void UpdateMaterials()
    {
        foreach (GhostBlock block in blocks.Values)
        {
            block.UpdateTransparency();
        }
    }
}

public class Ghost : MonoBehaviour
{
    public void OnDestroy()
    {
        GhostMan.UnRegister(gameObject);
    }
}

public class GhostBlock
{
    private static readonly int _MainTex = Shader.PropertyToID("_MainTex");
    private static readonly int _Color = Shader.PropertyToID("_Color");
    private static readonly int _FresnelPower = Shader.PropertyToID("_FresnelPower");
    private static readonly int _GhostPower = Shader.PropertyToID("_Power");

    private readonly Dictionary<Renderer, Material[]> materialMap = new();
    private readonly Dictionary<Renderer, RendererProps> props = new();
    private readonly List<Material> materials = new();
    private readonly Dictionary<Renderer, Material[]> newMaterialMap = new();

    private bool enabled;

    public GhostBlock(List<Renderer> renderers)
    {
        for (int i = 0; i < renderers.Count; ++i)
        {
            Renderer renderer = renderers[i];
            materialMap[renderer] = renderer.materials;
            SetupMaterials(renderer);
            SetupProps(renderer);
        }

        enabled = true;
        
        if (!GhostMan.enabled) Reset();
    }

    private void SetupMaterials(Renderer renderer)
    {
        List<Material> newMats = new();
        for (var i = 0; i < renderer.materials.Length; ++i)
        {
            Material mat = renderer.materials[i];
            Material material = new Material(GhostMan.GhostMat);
            if (material.HasProperty(_MainTex) && mat.HasProperty(_MainTex))
            {
                material.SetTexture(_MainTex, mat.GetTexture(_MainTex));
            }

            if (material.HasProperty(_Color))
            {
                material.color = new Color(ConfigManager.GhostTint.r, ConfigManager.GhostTint.g, ConfigManager.GhostTint.b, ConfigManager.GhostTransparency);
            }

            if (material.HasProperty(_FresnelPower))
            {
                material.SetFloat(_FresnelPower, ConfigManager.FresnelPower);
            }
            
            if (material.HasProperty(_GhostPower))
            {
                material.SetFloat(_GhostPower, ConfigManager.GhostPower);
            }
            newMats.Add(material);
        }

        renderer.materials = newMats.ToArray();
        newMaterialMap[renderer] = newMats.ToArray();
        materials.AddRange(newMats);
    }

    public void UpdateTransparency()
    {
        for (int i = 0; i < materials.Count; ++i)
        {
            Material mat = materials[i];
            if (mat.HasProperty(_Color))
            {
                mat.color = new Color(ConfigManager.GhostTint.r, ConfigManager.GhostTint.g, ConfigManager.GhostTint.b, ConfigManager.GhostTransparency);
            }

            if (mat.HasProperty(_FresnelPower))
            {
                mat.SetFloat(_FresnelPower, ConfigManager.FresnelPower);
            }

            if (mat.HasProperty(_GhostPower))
            {
                mat.SetFloat(_GhostPower, ConfigManager.GhostPower);
            }
        }
    }

    private void SetupProps(Renderer renderer)
    {
        RendererProps properties = new RendererProps();
        properties.shadowCastingMode = renderer.shadowCastingMode;
        properties.lightProbeUsage = renderer.lightProbeUsage;
        properties.layer = renderer.gameObject.layer;
        properties.receiveShadows = renderer.receiveShadows;
        props[renderer] = properties;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.gameObject.layer = LayerMask.NameToLayer("ghost");
        renderer.receiveShadows = false;
    }

    public void Reset()
    {
        if (!enabled) return;
        
        foreach (KeyValuePair<Renderer, Material[]> kvp in materialMap)
        {
            kvp.Key.materials = kvp.Value;
        }

        foreach (KeyValuePair<Renderer, RendererProps> kvp in props)
        {
            kvp.Key.shadowCastingMode = kvp.Value.shadowCastingMode;
            kvp.Key.receiveShadows = kvp.Value.receiveShadows;
            kvp.Key.lightProbeUsage = kvp.Value.lightProbeUsage;
            // kvp.Key.gameObject.layer = kvp.Value.layer;
        }

        enabled = false;
    }

    public void ReApply()
    {
        if (enabled) return;
        
        foreach (KeyValuePair<Renderer, Material[]> kvp in newMaterialMap)
        {
            kvp.Key.materials = kvp.Value;
            
            kvp.Key.shadowCastingMode = ShadowCastingMode.Off;
            kvp.Key.lightProbeUsage = LightProbeUsage.Off;
            // kvp.Key.gameObject.layer = LayerMask.NameToLayer("ghost");
            kvp.Key.receiveShadows = false;
        }

        enabled = true;
    }

    private class RendererProps
    {
        public ShadowCastingMode shadowCastingMode;
        public bool receiveShadows;
        public LightProbeUsage lightProbeUsage;
        public int layer;
    }
}