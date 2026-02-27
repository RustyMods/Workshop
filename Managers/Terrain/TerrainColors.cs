using System.Collections.Generic;
using UnityEngine;

namespace Workshop;

public class TerrainColors : MonoBehaviour
{
    private static readonly List<TerrainColors> instances = new();
    public int m_width;
    public bool[] m_modifiedTerrain;
    public Color32[] m_terrainMask;
    public bool m_initialized;
    
    public TerrainComp m_terrainComp;
    public ZNetView m_nview;
    
    public void Awake()
    {
        m_nview = GetComponent<ZNetView>();
        m_terrainComp = GetComponent<TerrainComp>();
        instances.Add(this);
    }

    public void OnDestroy()
    {
        instances.Remove(this);
    }

    public void Initialize()
    {
        m_width = m_terrainComp.m_hmap.m_width;
        int num = m_width + 1;
        m_modifiedTerrain = new bool[num * num];
        m_terrainMask = new Color32[num * num];
        m_initialized = true;
    }

    public void Save()
    {
        if (!m_initialized || !m_nview.IsValid() || !m_nview.IsOwner()) return;

        ZPackage pkg = new ZPackage();
        pkg.Write(m_modifiedTerrain.Length);
        for (int i = 0; i < m_modifiedTerrain.Length; ++i)
        {
            bool modified = m_modifiedTerrain[i];
            pkg.Write(modified);
            if (modified)
            {
                Color32 color = m_terrainMask[i];
                pkg.Write(color.r);
                pkg.Write(color.g);
                pkg.Write(color.b);
                pkg.Write(color.a);
            }
        }

        byte[] bytes = Utils.Compress(pkg.GetArray());
        m_nview.GetZDO().Set(PaintVars.TerrainColors, bytes);
    }

    public void Load()
    {
        byte[] bytes = m_nview.GetZDO().GetByteArray(PaintVars.TerrainColors);
        if (bytes == null) return;
        byte[] decompressed = Utils.Decompress(bytes);
        ZPackage pkg = new ZPackage(decompressed);
        int length = pkg.ReadInt();
        for (int i = 0; i < length; ++i)
        {
            bool modified = pkg.ReadBool();
            m_modifiedTerrain[i] = modified;
            if (modified)
            {
                byte r = pkg.ReadByte();
                byte g = pkg.ReadByte();
                byte b = pkg.ReadByte();
                byte a = pkg.ReadByte();
                m_terrainMask[i] = new Color32(r, g, b, a);
            }
            else
            {
                m_terrainMask[i] = new Color32();
            }
        }
    }

    public void ApplyToHeightmap(Heightmap hm)
    {
        if (!m_initialized) return;
        
        List<Color32> colors = new List<Color32>();
        hm.m_renderMesh.GetColors(colors);
            
        int num = hm.m_width + 1;
        for (int x = 0; x < num; ++x)
        {
            for (int y = 0; y < num; ++y)
            {
                int index = x * num + y;
                bool modified = m_modifiedTerrain[index];
                if (modified)
                {
                    Color32 color = m_terrainMask[index];
                    colors[index] = color;
                }
            }
        }
            
        hm.m_renderMesh.SetColors(colors);
    }

    public Heightmap.Biome GetBiome(Heightmap hm, Vector3 point)
    {
        hm.WorldToVertexMask(point, out int x, out int y);
        int index = x * (hm.m_width + 1) + y;
        bool modified = m_modifiedTerrain[index];
        if (!modified) return hm.GetBiome(point);
        Color32 color = m_terrainMask[index];
        if (color is { r: byte.MaxValue, a: byte.MaxValue }) return Heightmap.Biome.AshLands;
        if (color is { b: byte.MaxValue, a: byte.MaxValue }) return Heightmap.Biome.Mistlands;
        
        if (color.r == byte.MaxValue) return Heightmap.Biome.Swamp;
        if (color.g == byte.MaxValue) return Heightmap.Biome.Mountain | Heightmap.Biome.DeepNorth;
        if (color.b == byte.MaxValue) return Heightmap.Biome.BlackForest;
        if (color.a == byte.MaxValue) return Heightmap.Biome.Plains;
        return Heightmap.Biome.Meadows;
    }

    public static bool TryFindTerrainColors(Vector3 pos, out TerrainColors instance)
    {
        for (int i = 0; i < instances.Count; ++i)
        {
            TerrainColors terrain = instances[i];
            float halfSize = terrain.m_terrainComp.m_size / 2f;
            Vector3 position = terrain.transform.position;
            
            if (pos.x >= position.x - halfSize && 
                pos.x <= position.x + halfSize && 
                pos.z >= position.z - halfSize &&
                pos.z <= position.z + halfSize)
            {
                instance = terrain;
                return true;
            }
        }
        instance = null;
        return false;
    }

    public static TerrainColors FindTerrainColors(Vector3 pos)
    {
        for (int i = 0; i < instances.Count; ++i)
        {
            TerrainColors instance = instances[i];
            float halfSize = instance.m_terrainComp.m_size / 2f;
            Vector3 position = instance.transform.position;
            
            if (pos.x >= position.x - halfSize && 
                pos.x <= position.x + halfSize && 
                pos.z >= position.z - halfSize &&
                pos.z <= position.z + halfSize)
            {
                return instance;
            }
        }
        return null;
    }
}